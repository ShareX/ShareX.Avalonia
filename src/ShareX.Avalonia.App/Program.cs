#region License Information (GPL v3)

/*
    ShareX.Ava - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2025 ShareX Team

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
using System;

namespace ShareX.Ava.App
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            InitializePlatformServices();

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        private static void InitializePlatformServices()
        {
            if (OperatingSystem.IsWindows())
            {
                // Create Windows platform services
                var screenService = new ShareX.Ava.Platform.Windows.WindowsScreenService();
                
                // Create Windows capture service (GDI+)
                var winCaptureService = new ShareX.Ava.Platform.Windows.WindowsScreenCaptureService(screenService);
                
                // Create UI capture service (Wrapper with Region UI)
                // This delegates to winCaptureService for actual capture
                var uiCaptureService = new ShareX.Ava.UI.Services.ScreenCaptureService(winCaptureService);
                
                // Initialize Windows platform with our UI wrapper
                ShareX.Ava.Platform.Windows.WindowsPlatform.Initialize(uiCaptureService);
            }
            else
            {
                // Fallback for non-Windows (or generic stubs)
                // In future: LinuxPlatform.Initialize()
                System.Diagnostics.Debug.WriteLine("Warning: Non-Windows platform detected, services may not be fully functional.");
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<ShareX.Ava.UI.App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
