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

using ShareX.Avalonia.Common;
using ShareX.Avalonia.Core;
using ShareX.Avalonia.Core.Hotkeys;
using ShareX.Avalonia.Core.Managers;
using System;
using System.Threading.Tasks;

using ShareX.Avalonia.Platform.Abstractions;
using ShareX.Avalonia.Core.Tasks;
using System.Drawing;

namespace ShareX.Avalonia.Core.Helpers;

public static partial class TaskHelpers
{
    public static async Task ExecuteJob(HotkeyType job, TaskSettings? taskSettings = null)
    {
        DebugHelper.WriteLine($"Executing job: {job}");

        if (!PlatformServices.IsInitialized)
        {
            DebugHelper.WriteLine("Platform services not initialized.");
            return;
        }

        // Create default settings if none provided
        if (taskSettings == null)
        {
            taskSettings = new TaskSettings();
            
            // Apply job-specific defaults if needed
            if (taskSettings.Job == HotkeyType.None)
            {
                taskSettings.Job = job;
            }
        }

        // Ensure the job type in settings matches the requested job
        if (taskSettings.Job != job && job != HotkeyType.None)
        {
            taskSettings.Job = job;
        }

        try 
        {
            // Start the task via TaskManager
            // This ensures it appears in the UI and follows the standard lifecycle
            await TaskManager.Instance.StartTask(taskSettings);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex, $"Error starting job {job}");
        }
    }
}
