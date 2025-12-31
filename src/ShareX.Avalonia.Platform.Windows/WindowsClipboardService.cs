#region License Information (GPL v3)

/*
    ShareX.Avalonia - The Avalonia UI implementation of ShareX
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

using ShareX.Avalonia.Platform.Abstractions;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShareX.Avalonia.Platform.Windows
{
    /// <summary>
    /// Windows implementation of IClipboardService using System.Windows.Forms.Clipboard
    /// </summary>
    public class WindowsClipboardService : IClipboardService
    {
        public void Clear()
        {
            try
            {
                Clipboard.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear clipboard: {ex.Message}");
            }
        }

        public bool ContainsText()
        {
            try
            {
                return Clipboard.ContainsText();
            }
            catch
            {
                return false;
            }
        }

        public bool ContainsImage()
        {
            try
            {
                return Clipboard.ContainsImage();
            }
            catch
            {
                return false;
            }
        }

        public bool ContainsFileDropList()
        {
            try
            {
                return Clipboard.ContainsFileDropList();
            }
            catch
            {
                return false;
            }
        }

        public string? GetText()
        {
            try
            {
                return Clipboard.GetText();
            }
            catch
            {
                return null;
            }
        }

        public void SetText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set clipboard text: {ex.Message}");
            }
        }

        public Image? GetImage()
        {
            try
            {
                return Clipboard.GetImage();
            }
            catch
            {
                return null;
            }
        }

        public void SetImage(Image image)
        {
            if (image == null)
                return;

            // Windows clipboard operations may fail due to other applications
            // Implement retry logic with delays
            Exception? lastException = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Clipboard.SetImage(image);
                    return; // Success
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    System.Diagnostics.Debug.WriteLine($"Clipboard SetImage attempt {i + 1} failed: {ex.Message}");
                    
                    if (i < 4) // Don't sleep on last attempt
                    {
                        System.Threading.Thread.Sleep(100); // Wait 100ms before retry
                    }
                }
            }
            
            // If all retries failed, throw the exception
            throw new InvalidOperationException($"Failed to set clipboard image after 5 attempts: {lastException?.Message}", lastException);
        }

        public string[]? GetFileDropList()
        {
            try
            {
                var files = Clipboard.GetFileDropList();
                if (files != null && files.Count > 0)
                {
                    string[] result = new string[files.Count];
                    files.CopyTo(result, 0);
                    return result;
                }
            }
            catch
            {
                // Ignore
            }

            return null;
        }

        public void SetFileDropList(string[] files)
        {
            if (files == null || files.Length == 0)
                return;

            try
            {
                var fileCollection = new System.Collections.Specialized.StringCollection();
                fileCollection.AddRange(files);
                Clipboard.SetFileDropList(fileCollection);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set clipboard file drop list: {ex.Message}");
            }
        }

        public object? GetData(string format)
        {
            try
            {
                return Clipboard.GetData(format);
            }
            catch
            {
                return null;
            }
        }

        public void SetData(string format, object data)
        {
            if (string.IsNullOrEmpty(format) || data == null)
                return;

            try
            {
                Clipboard.SetData(format, data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set clipboard data: {ex.Message}");
            }
        }

        public bool ContainsData(string format)
        {
            if (string.IsNullOrEmpty(format))
                return false;

            try
            {
                return Clipboard.ContainsData(format);
            }
            catch
            {
                return false;
            }
        }

        public Task<string?> GetTextAsync()
        {
            // Windows Forms Clipboard is synchronous, but we provide async wrapper for consistency
            return Task.FromResult(GetText());
        }

        public Task SetTextAsync(string text)
        {
            SetText(text);
            return Task.CompletedTask;
        }
    }
}
