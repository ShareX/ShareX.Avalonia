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

using ShareX.Ava.Common;
using ShareX.Ava.Platform.Abstractions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ShareX.Ava.Platform.Windows
{
    /// <summary>
    /// Windows implementation of IWindowService using NativeMethods
    /// </summary>
    public class WindowsWindowService : IWindowService
    {
        public IntPtr GetForegroundWindow()
        {
            return NativeMethods.GetForegroundWindow();
        }

        public bool SetForegroundWindow(IntPtr handle)
        {
            return NativeMethods.SetForegroundWindow(handle);
        }

        public string GetWindowText(IntPtr handle)
        {
            return NativeMethods.GetWindowText(handle);
        }

        public string GetWindowClassName(IntPtr handle)
        {
            return NativeMethods.GetClassName(handle);
        }

        public Rectangle GetWindowBounds(IntPtr handle)
        {
            return NativeMethods.GetWindowRect(handle);
        }

        public Rectangle GetWindowClientBounds(IntPtr handle)
        {
            return NativeMethods.GetClientRect(handle);
        }

        public bool IsWindowVisible(IntPtr handle)
        {
            return NativeMethods.IsWindowVisible(handle);
        }

        public bool IsWindowMaximized(IntPtr handle)
        {
            return NativeMethods.IsZoomed(handle);
        }

        public bool IsWindowMinimized(IntPtr handle)
        {
            return NativeMethods.IsIconic(handle);
        }

        public bool ShowWindow(IntPtr handle, int cmdShow)
        {
            return NativeMethods.ShowWindow(handle, cmdShow);
        }

        public bool SetWindowPos(IntPtr handle, IntPtr handleInsertAfter, int x, int y, int width, int height, uint flags)
        {
            return NativeMethods.SetWindowPos(handle, handleInsertAfter, x, y, width, height, (SetWindowPosFlags)flags);
        }

        public Abstractions.WindowInfo[] GetAllWindows()
        {
            // This is a simplified implementation - for full implementation,
            // we would need to enumerate all top-level windows using EnumWindows
            var windows = new List<Abstractions.WindowInfo>();

            // For now, just return the foreground window as an example
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow != IntPtr.Zero)
            {
                windows.Add(new Abstractions.WindowInfo
                {
                    Handle = foregroundWindow,
                    Title = GetWindowText(foregroundWindow),
                    ClassName = GetWindowClassName(foregroundWindow),
                    Bounds = GetWindowBounds(foregroundWindow),
                    ProcessId = GetWindowProcessId(foregroundWindow),
                    IsVisible = IsWindowVisible(foregroundWindow),
                    IsMaximized = IsWindowMaximized(foregroundWindow),
                    IsMinimized = IsWindowMinimized(foregroundWindow)
                });
            }

            return windows.ToArray();
        }

        public uint GetWindowProcessId(IntPtr handle)
        {
            NativeMethods.GetWindowThreadProcessId(handle, out uint processId);
            return processId;
        }
    }
}
