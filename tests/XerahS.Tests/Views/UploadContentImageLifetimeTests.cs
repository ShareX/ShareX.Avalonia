#region License Information (GPL v3)

/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using NUnit.Framework;
using SkiaSharp;
using XerahS.Bootstrap;
using XerahS.Common;
using XerahS.Core;
using XerahS.Core.Tasks;
using XerahS.UI.ViewModels;
using XerahS.Uploaders;

namespace XerahS.Tests.Views;

[TestFixture]
[NonParallelizable]
public sealed class UploadContentImageLifetimeTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task UploadKeepsQueueImageAndDetachesProgressOnReturn(bool fail)
    {
        using var manager = new CompletingTaskManager(fail);
        using var viewModel = new UploadContentViewModel(manager);
        var original = new SKBitmap(32, 32);
        original.Erase(SKColors.Blue);
        var item = new UploadQueueItem { DataType = EDataType.Image, Image = original };
        viewModel.Items.Add(item);

        await viewModel.UploadAllCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(manager.Input, Is.Not.Null);
            Assert.That(manager.Input, Is.Not.SameAs(original));
            Assert.That(manager.Input!.Handle, Is.EqualTo(IntPtr.Zero));
            Assert.That(original.Handle, Is.Not.EqualTo(IntPtr.Zero));
            Assert.That(original.GetPixel(0, 0), Is.EqualTo(SKColors.Blue));
        });

        int finishedProgress = item.ProgressPercent;
        manager.LastTask!.Info.ReportUploadProgress(new ProgressManager(100, 91));
        Assert.That(item.ProgressPercent, Is.EqualTo(finishedProgress),
            "Retained task history must no longer subscribe to the upload queue item.");
    }

    private sealed class CompletingTaskManager(bool fail) : IDesktopTaskManager, IDisposable
    {
        public event EventHandler<WorkerTask>? TaskStarted;
        public event EventHandler<WorkerTask>? TaskCompleted { add { } remove { } }
        public SKBitmap? Input { get; private set; }
        public WorkerTask? LastTask { get; private set; }
        public IEnumerable<WorkerTask> Tasks => LastTask == null ? [] : [LastTask];

        public Task StartTask(TaskSettings? settings, SKBitmap? inputImage = null)
        {
            Input = inputImage;
            LastTask = WorkerTask.Create(settings ?? new TaskSettings(), inputImage);
            TaskStarted?.Invoke(this, LastTask);
            LastTask.Info.ReportUploadProgress(new ProgressManager(100, 25));
            // Simulate the worker releasing its owned pixels on success or failure.
            LastTask.Dispose();
            return fail ? Task.FromException(new InvalidOperationException("test failure")) : Task.CompletedTask;
        }

        public Task StartFileTask(TaskSettings? settings, string path) => throw new NotSupportedException();
        public Task StartImageUploadTask(TaskSettings? settings, SKBitmap image) => StartTask(settings, image);
        public Task StartTextTask(TaskSettings? settings, string text) => throw new NotSupportedException();
        public void StopAllTasks() { }
        public void Dispose() => LastTask?.Dispose();
    }
}
