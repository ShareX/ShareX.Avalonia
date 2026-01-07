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

using System;
using Microsoft.Toolkit.Uwp.Notifications;
using ShareX.Ava.Services.Abstractions;

namespace ShareX.Ava.Platform.Windows;

/// <summary>
/// Windows-specific notification service using Windows Toast Notifications (UWP).
/// </summary>
public class WindowsNotificationService : INotificationService, IDisposable
{
    public WindowsNotificationService()
    {
    }

    public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
    {
        try
        {
            // Note: For this to work, the app must have an AUMID registered or be packaged.
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Windows Notification Error] {ex.Message}");
        }
    }

    public void ShowNotification(string title, string message, string actionText, Action action, NotificationType type = NotificationType.Info)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                // Buttons require more setup for handling clicks (ToastNotificationManagerCompat.OnActivated)
                // For this iteration we settle for showing the button visual.
                .AddButton(new ToastButton()
                    .SetContent(actionText)
                    .AddArgument("action", "click"))
                .Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Windows Notification Error] {ex.Message}");
        }
    }

    public void Dispose()
    {
        // Microsoft.Toolkit.Uwp.Notifications 7.1.x: Uninstall() is on ToastNotificationManagerCompat
        // But if the method is missing in the dll version we got, we skip it.
        // It generally shouldn't be missing if TFM is correct.
        try 
        {
             ToastNotificationManagerCompat.Uninstall();
        }
        catch 
        { 
             // Intentionally ignored
        }
    }
}
