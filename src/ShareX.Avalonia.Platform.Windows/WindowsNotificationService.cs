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
/// Windows-specific notification service using Windows Toast Notifications.
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
                .AddButton(new ToastButton()
                    .SetContent(actionText)
                    .AddArgument("action", "click"))
                .Show();
            
            // Note: Full action handling requires ToastNotificationManagerCompat.OnActivated subscription
            // For now, we show the notification - action handling can be added in a later iteration
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Windows Notification Error] {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            // Clean up notification manager on dispose
            ToastNotificationManagerCompat.Uninstall();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
