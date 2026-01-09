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

using XerahS.Common;
using XerahS.Core.Managers;

using XerahS.Platform.Abstractions;

namespace XerahS.Core.Helpers;

public static partial class TaskHelpers
{
    /// <summary>
    /// Execute a workflow using its complete settings.
    /// This is the preferred method - avoids ambiguity with HotkeyType lookup.
    /// </summary>
    public static async Task ExecuteWorkflow(Core.Hotkeys.WorkflowSettings workflow)
    {
        TroubleshootingHelper.Log(workflow?.Job.ToString() ?? "Unknown", "EXECUTE_WORKFLOW", $"Entry: workflow={workflow?.Name ?? "null"}");
        
        if (workflow == null)
        {
            DebugHelper.WriteLine("ExecuteWorkflow: workflow is null");
            TroubleshootingHelper.Log("Unknown", "EXECUTE_WORKFLOW", "ABORT: workflow is null");
            return;
        }

        TroubleshootingHelper.Log(workflow.Job.ToString(), "EXECUTE_WORKFLOW", $"Calling ExecuteJob, TaskSettings={workflow.TaskSettings != null}");
        await ExecuteJob(workflow.Job, workflow.TaskSettings);
    }

    public static async Task ExecuteJob(HotkeyType job, TaskSettings? taskSettings = null)
    {
        TroubleshootingHelper.Log(job.ToString(), "EXECUTE_JOB", $"Entry: taskSettings={taskSettings != null}");
        DebugHelper.WriteLine($"Executing job: {job}");

        if (!PlatformServices.IsInitialized)
        {
            DebugHelper.WriteLine("Platform services not initialized.");
            TroubleshootingHelper.Log(job.ToString(), "EXECUTE_JOB", "ABORT: Platform services not initialized");
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

        if (taskSettings.Job != job && job != HotkeyType.None)
        {
            taskSettings.Job = job;
        }

        DebugHelper.WriteLine(
            $"Task settings: AfterCaptureJob={taskSettings.AfterCaptureJob}, " +
            $"UploadImageToHost={taskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.UploadImageToHost)}, " +
            $"ImageDestination={taskSettings.ImageDestination}");

        try
        {
            // Start the task via TaskManager
            // This ensures it appears in the UI and follows the standard lifecycle
            TroubleshootingHelper.Log(taskSettings.Job.ToString(), "EXECUTE_JOB", "Calling TaskManager.StartTask");
            await TaskManager.Instance.StartTask(taskSettings);
            TroubleshootingHelper.Log(taskSettings.Job.ToString(), "EXECUTE_JOB", "TaskManager.StartTask completed");
        }
        catch (Exception ex)
        {
            TroubleshootingHelper.Log(taskSettings?.Job.ToString() ?? "Unknown", "EXECUTE_JOB", $"ERROR: {ex.Message}");
            DebugHelper.WriteException(ex, $"Error starting job {job}");
        }
    }
}
