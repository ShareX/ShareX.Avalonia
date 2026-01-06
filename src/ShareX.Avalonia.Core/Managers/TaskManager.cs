using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShareX.Ava.Core;
using ShareX.Ava.Core.Tasks;
using ShareX.Ava.Common;

namespace ShareX.Ava.Core.Managers
{
    public class TaskManager
    {
        private static readonly Lazy<TaskManager> _lazy = new(() => new TaskManager());
        public static TaskManager Instance => _lazy.Value;

        private readonly ConcurrentBag<WorkerTask> _tasks = new();
        public IEnumerable<WorkerTask> Tasks => _tasks;

        private TaskManager()
        {
        }

        // Event fired when a task completes with an image
        public event EventHandler<WorkerTask>? TaskCompleted;

        public async Task StartTask(TaskSettings taskSettings, SkiaSharp.SKBitmap? inputImage = null)
        {
            var task = WorkerTask.Create(taskSettings, inputImage);
            _tasks.Add(task);

            task.StatusChanged += (s, e) => DebugHelper.WriteLine($"Task Status: {task.Status}");
            task.TaskCompleted += (s, e) =>
            {
                // Fire event so listeners (like App.axaml.cs) can update UI
                TaskCompleted?.Invoke(this, task);
            };
            
            await task.StartAsync();
        }

        public void StopAllTasks()
        {
            foreach (var task in _tasks.Where(t => t.IsWorking))
            {
                task.Stop();
            }
        }
    }
}
