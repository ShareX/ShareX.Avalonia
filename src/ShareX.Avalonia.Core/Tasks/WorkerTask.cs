using System;
using System.Threading;
using System.Threading.Tasks;
using ShareX.Ava.Core;

using ShareX.Ava.Common;

using ShareX.Ava.Core.Tasks.Processors;
using ShareX.Ava.Platform.Abstractions;

namespace ShareX.Ava.Core.Tasks
{
    public class WorkerTask
    {
        public TaskInfo Info { get; private set; }
        public TaskStatus Status { get; private set; }
        public bool IsBusy => Status == TaskStatus.InQueue || IsWorking;
        public bool IsWorking => Status == TaskStatus.Preparing || Status == TaskStatus.Working || Status == TaskStatus.Stopping;
        
        private CancellationTokenSource _cancellationTokenSource;

        public event EventHandler StatusChanged;
        public event EventHandler TaskCompleted;

        private WorkerTask(TaskSettings taskSettings)
        {
            Status = TaskStatus.InQueue;
            Info = new TaskInfo(taskSettings);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public static WorkerTask Create(TaskSettings taskSettings)
        {
            return new WorkerTask(taskSettings);
        }

        public async Task StartAsync()
        {
            if (Status != TaskStatus.InQueue) return;

            Info.TaskStartTime = DateTime.Now;
            Status = TaskStatus.Preparing;
            OnStatusChanged();

            try
            {
                await Task.Run(async () => await DoWorkAsync(_cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                Status = TaskStatus.Stopped;
            }
            catch (Exception ex)
            {
                Status = TaskStatus.Failed;
                DebugHelper.WriteLine($"Task failed: {ex.Message}");
            }
            finally
            {
                if (Status != TaskStatus.Failed && Status != TaskStatus.Stopped)
                {
                    Status = TaskStatus.Completed;
                }
                
                OnTaskCompleted();
                OnStatusChanged();
            }
        }

        private async Task DoWorkAsync(CancellationToken token)
        {
            Status = TaskStatus.Working;
            OnStatusChanged();

            // Perform Capture Phase based on Job Type
            if (PlatformServices.IsInitialized)
            {
                System.Drawing.Image? image = null;
                
                switch (Info.TaskSettings.Job)
                {
                    case HotkeyType.PrintScreen:
                        image = await PlatformServices.ScreenCapture.CaptureFullScreenAsync();
                        break;
                        
                    case HotkeyType.RectangleRegion:
                        image = await PlatformServices.ScreenCapture.CaptureRegionAsync();
                        break;
                        
                    case HotkeyType.ActiveWindow:
                        if (PlatformServices.Window != null)
                        {
                            image = await PlatformServices.ScreenCapture.CaptureActiveWindowAsync(PlatformServices.Window);
                        }
                        break;
                }
                
                if (image is System.Drawing.Bitmap bitmap)
                {
                    Info.Metadata.Image = bitmap;
                    DebugHelper.WriteLine($"Captured image: {bitmap.Width}x{bitmap.Height}");
                }
                else if (image != null)
                {
                    // Convert to Bitmap if it's a different Image type
                    Info.Metadata.Image = new System.Drawing.Bitmap(image);
                    DebugHelper.WriteLine($"Converted image to Bitmap: {image.Width}x{image.Height}");
                }
                else
                {
                    DebugHelper.WriteLine($"Capture returned null for job type: {Info.TaskSettings.Job}");
                }
            }
            else
            {
                DebugHelper.WriteLine("PlatformServices not initialized - cannot capture");
            }

            // Execute Capture Job (File Save, Clipboard, etc)
            var captureProcessor = new CaptureJobProcessor();
            await captureProcessor.ProcessAsync(Info, token);

            // Execute Upload Job
            var uploadProcessor = new UploadJobProcessor();
            await uploadProcessor.ProcessAsync(Info, token);
        }

        public void Stop()
        {
            if (IsWorking)
            {
                Status = TaskStatus.Stopping;
                OnStatusChanged();
                _cancellationTokenSource.Cancel();
            }
        }

        protected virtual void OnStatusChanged()
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnTaskCompleted()
        {
            TaskCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
