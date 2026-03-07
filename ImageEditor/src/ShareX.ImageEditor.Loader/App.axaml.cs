#region License Information (GPL v3)

/*
    ShareX.ImageEditor - The UI-agnostic Editor library for ShareX
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

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Presentation.ViewModels;
using SkiaSharp;
using System;
using System.IO;

namespace ShareX.ImageEditor.Loader
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

#if DEBUG
            this.AttachDeveloperTools();
#endif
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = new ShareX.ImageEditor.Presentation.Views.EditorWindow();
                desktop.MainWindow = window;

                if (window.DataContext is MainViewModel vm)
                {
                    vm.TaskMode = true;
                    LoadExampleImage(vm);
                    //GenerateSampleImage(vm, 4000, 2000);

                    vm.CopyRequested += async () =>
                    {
                        var clipboard = desktop.MainWindow?.Clipboard;

                        if (clipboard != null)
                        {
                            var bytes = window.GetResultBytes();

                            if (bytes != null)
                            {
                                using (var stream = new MemoryStream(bytes))
                                {
                                    var bitmap = new Avalonia.Media.Imaging.Bitmap(stream);
                                    var data = new DataTransfer();
                                    var item = new DataTransferItem();
                                    item.SetBitmap(bitmap);
                                    data.Add(item);
                                    await clipboard.SetDataAsync(data);
                                }
                            }
                        }
                    };
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void LoadExampleImage(MainViewModel vm)
        {
            string location = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(location, "Assets", "Sample.png");

            if (File.Exists(path))
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    SKBitmap skBitmap = SKBitmap.Decode(stream);

                    vm.UpdatePreview(skBitmap);
                }
            }
        }

        private void GenerateSampleImage(MainViewModel vm, int width, int height)
        {
            SKImageInfo info = new SKImageInfo(width, height);
            SKBitmap skBitmap = new SKBitmap(info);

            using (SKCanvas canvas = new SKCanvas(skBitmap))
            {
                canvas.Clear(SKColors.Transparent);

                using (SKPaint paint = new SKPaint { Color = SKColors.LightBlue, IsAntialias = true })
                {
                    canvas.DrawCircle(width / 2, height / 2, width / 3, paint);
                }
            }

            vm.UpdatePreview(skBitmap);
        }
    }
}