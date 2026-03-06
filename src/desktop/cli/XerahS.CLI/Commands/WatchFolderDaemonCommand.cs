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

using System.CommandLine;
using XerahS.Core;
using XerahS.Platform.Abstractions;

namespace XerahS.CLI.Commands
{
    public static class WatchFolderDaemonCommand
    {
        public static Command Create()
        {
            var daemonCommand = new Command("watchfolder-daemon", "Manage the XerahS watch folder daemon");

            var scopeOption = new Option<string>("--scope") { Description = "Daemon scope: 'user' or 'system'" };
            var startAtStartupOption = new Option<bool>("--start-at-startup") { Description = "Enable automatic startup when installed" };

            // status subcommand
            var statusCommand = new Command("status", "Show daemon status");
            statusCommand.Add(scopeOption);
            statusCommand.SetAction((parseResult) =>
            {
                string scopeStr = parseResult.GetValue(scopeOption) ?? "user";
                Environment.ExitCode = StatusAsync(ParseScope(scopeStr)).GetAwaiter().GetResult();
            });

            // start subcommand
            var startCommand = new Command("start", "Install and start the daemon");
            startCommand.Add(scopeOption);
            startCommand.Add(startAtStartupOption);
            startCommand.SetAction((parseResult) =>
            {
                string scopeStr = parseResult.GetValue(scopeOption) ?? "user";
                bool startAtStartup = parseResult.GetValue(startAtStartupOption);
                Environment.ExitCode = StartAsync(ParseScope(scopeStr), startAtStartup).GetAwaiter().GetResult();
            });

            // stop subcommand
            var stopCommand = new Command("stop", "Stop the daemon");
            stopCommand.Add(scopeOption);
            stopCommand.SetAction((parseResult) =>
            {
                string scopeStr = parseResult.GetValue(scopeOption) ?? "user";
                Environment.ExitCode = StopAsync(ParseScope(scopeStr)).GetAwaiter().GetResult();
            });

            daemonCommand.Add(statusCommand);
            daemonCommand.Add(startCommand);
            daemonCommand.Add(stopCommand);

            return daemonCommand;
        }

        private static WatchFolderDaemonScope ParseScope(string scopeStr)
        {
            return scopeStr.Equals("system", StringComparison.OrdinalIgnoreCase)
                ? WatchFolderDaemonScope.System
                : WatchFolderDaemonScope.User;
        }

        private static async Task<int> StatusAsync(WatchFolderDaemonScope scope)
        {
            if (!PlatformServices.IsInitialized)
            {
                Console.Error.WriteLine("Platform services not initialized.");
                return 1;
            }

            IWatchFolderDaemonService daemonService = PlatformServices.WatchFolderDaemon;
            if (!daemonService.IsSupported)
            {
                Console.Error.WriteLine("Watch folder daemon is not supported on this platform.");
                return 1;
            }

            WatchFolderDaemonStatus status = await daemonService.GetStatusAsync(scope);
            Console.WriteLine($"Scope:          {status.Scope}");
            Console.WriteLine($"State:          {status.State}");
            Console.WriteLine($"Installed:      {status.Installed}");
            Console.WriteLine($"Start at login: {status.StartAtStartup}");
            Console.WriteLine($"Message:        {status.Message}");
            return 0;
        }

        private static async Task<int> StartAsync(WatchFolderDaemonScope scope, bool startAtStartup)
        {
            if (!PlatformServices.IsInitialized)
            {
                Console.Error.WriteLine("Platform services not initialized.");
                return 1;
            }

            IWatchFolderDaemonService daemonService = PlatformServices.WatchFolderDaemon;
            if (!daemonService.IsSupported)
            {
                Console.Error.WriteLine("Watch folder daemon is not supported on this platform.");
                return 1;
            }

            string settingsFolder = SettingsManager.PersonalFolder;
            Console.WriteLine($"Starting daemon (scope={scope}, settings={settingsFolder}, startAtStartup={startAtStartup})...");

            WatchFolderDaemonResult result = await daemonService.StartAsync(scope, settingsFolder, startAtStartup);
            if (result.Success)
            {
                Console.WriteLine($"OK: {result.Message}");
                return 0;
            }

            Console.Error.WriteLine($"FAILED ({result.ErrorCode}): {result.Message}");
            return 1;
        }

        private static async Task<int> StopAsync(WatchFolderDaemonScope scope)
        {
            if (!PlatformServices.IsInitialized)
            {
                Console.Error.WriteLine("Platform services not initialized.");
                return 1;
            }

            IWatchFolderDaemonService daemonService = PlatformServices.WatchFolderDaemon;
            if (!daemonService.IsSupported)
            {
                Console.Error.WriteLine("Watch folder daemon is not supported on this platform.");
                return 1;
            }

            Console.WriteLine($"Stopping daemon (scope={scope})...");
            WatchFolderDaemonResult result = await daemonService.StopAsync(scope, TimeSpan.FromSeconds(30));
            if (result.Success)
            {
                Console.WriteLine($"OK: {result.Message}");
                return 0;
            }

            Console.Error.WriteLine($"FAILED ({result.ErrorCode}): {result.Message}");
            return 1;
        }
    }
}
