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

using System.Diagnostics;
using XerahS.Platform.Abstractions;

namespace XerahS.Platform.Linux.Services;

public sealed class LinuxWatchFolderDaemonService : WatchFolderDaemonServiceBase
{
    private const string UnitName = "xerahs-watchfolder.service";
    private const string LegacyUnitName = "watchfolder.service";
    private static readonly string[] KnownUnitNames = { UnitName, LegacyUnitName };

    public override bool IsSupported => true;

    public override bool SupportsScope(WatchFolderDaemonScope scope)
    {
        return scope == WatchFolderDaemonScope.User || scope == WatchFolderDaemonScope.System;
    }

    public override async Task<WatchFolderDaemonStatus> GetStatusAsync(WatchFolderDaemonScope scope, CancellationToken cancellationToken = default)
    {
        if (!SupportsScope(scope))
        {
            return new WatchFolderDaemonStatus
            {
                Scope = scope,
                State = WatchFolderDaemonState.Unknown,
                Installed = false,
                Message = "Unsupported daemon scope."
            };
        }

        string[] installedUnits = GetInstalledUnitNames(scope);
        if (installedUnits.Length == 0)
        {
            return new WatchFolderDaemonStatus
            {
                Scope = scope,
                State = WatchFolderDaemonState.NotInstalled,
                Installed = false,
                StartAtStartup = false,
                Message = "systemd unit is not installed."
            };
        }

        bool isRunning = false;
        bool startAtStartup = false;
        foreach (string unitName in KnownUnitNames)
        {
            if (await IsUnitActiveAsync(scope, unitName, cancellationToken))
            {
                isRunning = true;
            }

            if (await IsUnitEnabledAsync(scope, unitName, cancellationToken))
            {
                startAtStartup = true;
            }
        }

        return new WatchFolderDaemonStatus
        {
            Scope = scope,
            State = isRunning ? WatchFolderDaemonState.Running : WatchFolderDaemonState.Stopped,
            Installed = true,
            StartAtStartup = startAtStartup,
            Message = isRunning ? "Daemon is running." : "Daemon is stopped."
        };
    }

    public override async Task<WatchFolderDaemonResult> StartAsync(
        WatchFolderDaemonScope scope,
        string settingsFolder,
        bool startAtStartup,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsScope(scope))
        {
            return WatchFolderDaemonResult.Fail(WatchFolderDaemonErrorCode.UnsupportedScope, "Unsupported daemon scope.");
        }

        if (string.IsNullOrWhiteSpace(settingsFolder))
        {
            return WatchFolderDaemonResult.Fail(WatchFolderDaemonErrorCode.ValidationError, "Settings folder is required.");
        }

        string daemonPath = ResolveDaemonPath(
            new[] { "xerahs-watchfolder-daemon", "XerahS.WatchFolder.Daemon" },
            "xerahs-watchfolder-daemon");

        if (!File.Exists(daemonPath))
        {
            return WatchFolderDaemonResult.Fail(
                WatchFolderDaemonErrorCode.ValidationError,
                $"Daemon executable was not found: {daemonPath}");
        }

        if (scope == WatchFolderDaemonScope.System && !new LinuxPlatformInfo().IsElevated)
        {
            return await StartSystemScopeWithElevationAsync(daemonPath, settingsFolder, startAtStartup, cancellationToken);
        }

        var ensureResult = await EnsureUnitFilesAsync(scope, daemonPath, settingsFolder, cancellationToken);
        if (!ensureResult.Success)
        {
            return ensureResult;
        }

        var reloadResult = await RunSystemctlAsync(scope, "daemon-reload", cancellationToken);
        if (!reloadResult.IsSuccess)
        {
            return WatchFolderDaemonResult.Fail(WatchFolderDaemonErrorCode.CommandFailed, reloadResult.Output);
        }

        var enableResult = await ConfigureStartupUnitsAsync(scope, startAtStartup, cancellationToken);
        if (!enableResult.IsSuccess)
        {
            return WatchFolderDaemonResult.Fail(WatchFolderDaemonErrorCode.CommandFailed, enableResult.Output);
        }

        await StopLegacyUnitIfPresentAsync(scope, cancellationToken);

        var startResult = await RunSystemctlAsync(scope, $"start {UnitName}", cancellationToken);
        if (!startResult.IsSuccess)
        {
            return WatchFolderDaemonResult.Fail(WatchFolderDaemonErrorCode.CommandFailed, startResult.Output);
        }

        return WatchFolderDaemonResult.Ok("Daemon started.");
    }

    public override async Task<WatchFolderDaemonResult> StopAsync(
        WatchFolderDaemonScope scope,
        TimeSpan gracefulTimeout,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsScope(scope))
        {
            return WatchFolderDaemonResult.Fail(WatchFolderDaemonErrorCode.UnsupportedScope, "Unsupported daemon scope.");
        }

        string[] installedUnits = GetInstalledUnitNames(scope);
        if (installedUnits.Length == 0)
        {
            return WatchFolderDaemonResult.Ok("Daemon unit is not installed.");
        }

        if (scope == WatchFolderDaemonScope.System && !new LinuxPlatformInfo().IsElevated)
        {
            return await StopSystemScopeWithElevationAsync(cancellationToken);
        }

        foreach (string unitName in installedUnits)
        {
            var stopResult = await RunSystemctlAsync(scope, $"stop {unitName}", cancellationToken);
            if (!stopResult.IsSuccess && !CanIgnoreUnitMissingOrInactiveError(stopResult.Output))
            {
                return WatchFolderDaemonResult.Fail(WatchFolderDaemonErrorCode.CommandFailed, stopResult.Output);
            }
        }

        var timeout = gracefulTimeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : gracefulTimeout;
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            bool anyRunning = false;
            foreach (string unitName in installedUnits)
            {
                if (await IsUnitActiveAsync(scope, unitName, cancellationToken))
                {
                    anyRunning = true;
                    break;
                }
            }

            if (!anyRunning)
            {
                return WatchFolderDaemonResult.Ok("Daemon stopped.");
            }

            await Task.Delay(DefaultPollIntervalMs, cancellationToken);
        }

        return WatchFolderDaemonResult.Fail(WatchFolderDaemonErrorCode.CommandFailed, "Daemon did not stop before timeout.");
    }

    private static string GetUnitFilePath(WatchFolderDaemonScope scope, string unitName)
    {
        if (scope == WatchFolderDaemonScope.System)
        {
            return Path.Combine("/etc/systemd/system", unitName);
        }

        string? xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        string configHome = !string.IsNullOrWhiteSpace(xdgConfigHome)
            ? xdgConfigHome
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        return Path.Combine(configHome, "systemd", "user", unitName);
    }

    private static string[] GetInstalledUnitNames(WatchFolderDaemonScope scope)
    {
        var installed = new List<string>();
        foreach (string unitName in KnownUnitNames)
        {
            if (File.Exists(GetUnitFilePath(scope, unitName)))
            {
                installed.Add(unitName);
            }
        }

        return installed.ToArray();
    }

    private static async Task<WatchFolderDaemonResult> EnsureUnitFilesAsync(
        WatchFolderDaemonScope scope,
        string daemonPath,
        string settingsFolder,
        CancellationToken cancellationToken)
    {
        try
        {
            string content = BuildUnitFileContent(scope, daemonPath, settingsFolder);
            foreach (string unitName in KnownUnitNames)
            {
                string unitPath = GetUnitFilePath(scope, unitName);
                string? directory = Path.GetDirectoryName(unitPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(unitPath, content, cancellationToken);
            }

            return WatchFolderDaemonResult.Ok();
        }
        catch (Exception ex)
        {
            return WatchFolderDaemonResult.Fail(WatchFolderDaemonErrorCode.CommandFailed, ex.Message);
        }
    }

    private static async Task<WatchFolderDaemonResult> StartSystemScopeWithElevationAsync(
        string daemonPath,
        string settingsFolder,
        bool startAtStartup,
        CancellationToken cancellationToken)
    {
        string tempPrimaryUnitPath = Path.GetTempFileName();
        string tempLegacyUnitPath = Path.GetTempFileName();
        try
        {
            string unitContent = BuildUnitFileContent(WatchFolderDaemonScope.System, daemonPath, settingsFolder);
            await File.WriteAllTextAsync(tempPrimaryUnitPath, unitContent, cancellationToken);
            await File.WriteAllTextAsync(tempLegacyUnitPath, unitContent, cancellationToken);

            string primaryUnitPath = GetUnitFilePath(WatchFolderDaemonScope.System, UnitName);
            string legacyUnitPath = GetUnitFilePath(WatchFolderDaemonScope.System, LegacyUnitName);
            string enableCommand = startAtStartup ? "enable" : "disable";
            string script = $"""
                             set -e
                             cp '{EscapeShellSingleQuotedString(tempPrimaryUnitPath)}' '{EscapeShellSingleQuotedString(primaryUnitPath)}'
                             cp '{EscapeShellSingleQuotedString(tempLegacyUnitPath)}' '{EscapeShellSingleQuotedString(legacyUnitPath)}'
                             chmod 644 '{EscapeShellSingleQuotedString(primaryUnitPath)}'
                             chmod 644 '{EscapeShellSingleQuotedString(legacyUnitPath)}'
                             systemctl daemon-reload
                             systemctl {enableCommand} '{EscapeShellSingleQuotedString(UnitName)}'
                             systemctl disable '{EscapeShellSingleQuotedString(LegacyUnitName)}' || true
                             systemctl stop '{EscapeShellSingleQuotedString(LegacyUnitName)}' || true
                             systemctl start '{EscapeShellSingleQuotedString(UnitName)}'
                             """;

            CommandResult privilegedResult = await RunPrivilegedShellScriptAsync(
                script, RunPrivilegedProcessAsync, cancellationToken);

            if (privilegedResult.IsSuccess)
            {
                return WatchFolderDaemonResult.Ok("Daemon started.");
            }

            if (IsElevationDenied(privilegedResult.Output))
            {
                return WatchFolderDaemonResult.Fail(
                    WatchFolderDaemonErrorCode.RequiresElevation,
                    "Root privileges were not granted for System scope.");
            }

            return WatchFolderDaemonResult.Fail(WatchFolderDaemonErrorCode.CommandFailed, privilegedResult.Output);
        }
        catch (Exception ex)
        {
            return WatchFolderDaemonResult.Fail(WatchFolderDaemonErrorCode.CommandFailed, ex.Message);
        }
        finally
        {
            try
            {
                File.Delete(tempPrimaryUnitPath);
                File.Delete(tempLegacyUnitPath);
            }
            catch
            {
            }
        }
    }

    private static async Task<WatchFolderDaemonResult> StopSystemScopeWithElevationAsync(CancellationToken cancellationToken)
    {
        foreach (string unitName in KnownUnitNames)
        {
            CommandResult stopResult = await RunPrivilegedProcessAsync("systemctl", new[] { "stop", unitName }, cancellationToken);
            if (stopResult.IsSuccess || CanIgnoreUnitMissingOrInactiveError(stopResult.Output))
            {
                continue;
            }

            if (IsElevationDenied(stopResult.Output))
            {
                return WatchFolderDaemonResult.Fail(
                    WatchFolderDaemonErrorCode.RequiresElevation,
                    "Root privileges were not granted for System scope.");
            }

            return WatchFolderDaemonResult.Fail(WatchFolderDaemonErrorCode.CommandFailed, stopResult.Output);
        }

        return WatchFolderDaemonResult.Ok("Daemon stopped.");
    }

    private static string BuildUnitFileContent(
        WatchFolderDaemonScope scope,
        string daemonPath,
        string settingsFolder)
    {
        string wantedBy = scope == WatchFolderDaemonScope.System ? "multi-user.target" : "default.target";
        string escapedDaemonPath = daemonPath.Replace("\"", "\\\"");
        string escapedSettingsFolder = settingsFolder.Replace("\"", "\\\"");

        return $"""
                [Unit]
                Description=XerahS Watch Folder Daemon
                After=network.target

                [Service]
                Type=simple
                ExecStart="{escapedDaemonPath}" --scope {scope.ToString().ToLowerInvariant()} --settings-folder "{escapedSettingsFolder}"
                Restart=on-failure
                RestartSec=3
                KillSignal=SIGTERM
                TimeoutStopSec=30

                [Install]
                WantedBy={wantedBy}
                """;
    }

    private static async Task<CommandResult> RunPrivilegedProcessAsync(
        string fileName,
        string[] arguments,
        CancellationToken cancellationToken)
    {
        var pkexecArguments = new List<string> { fileName };
        pkexecArguments.AddRange(arguments);

        CommandResult pkexecResult = await RunProcessWithArgumentsAsync(
            "pkexec",
            pkexecArguments,
            cancellationToken,
            DefaultElevatedCommandTimeoutMs);

        if (pkexecResult.IsSuccess || !IsExecutableNotFound(pkexecResult.Output))
        {
            return pkexecResult;
        }

        var sudoArguments = new List<string> { fileName };
        sudoArguments.AddRange(arguments);

        CommandResult sudoResult = await RunProcessWithArgumentsAsync(
            "sudo",
            sudoArguments,
            cancellationToken,
            DefaultElevatedCommandTimeoutMs);

        if (sudoResult.IsSuccess || !IsExecutableNotFound(sudoResult.Output))
        {
            return sudoResult;
        }

        return new CommandResult(false, "Neither pkexec nor sudo is available for privileged daemon operations.");
    }

    private static bool IsExecutableNotFound(string output)
    {
        return output.Contains("No such file or directory", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("not found", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsElevationDenied(string output)
    {
        return output.Contains("not authorized", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("authorization failed", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("authentication failed", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("permission denied", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("a terminal is required", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("no tty present", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("canceled", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("cancelled", StringComparison.OrdinalIgnoreCase);
    }

    private static Task<CommandResult> RunSystemctlAsync(
        WatchFolderDaemonScope scope,
        string arguments,
        CancellationToken cancellationToken)
    {
        string fullArguments = scope == WatchFolderDaemonScope.User
            ? $"--user {arguments}"
            : arguments;

        return RunProcessAsync("systemctl", fullArguments, cancellationToken, DefaultCommandTimeoutMs);
    }

    private static async Task<CommandResult> ConfigureStartupUnitsAsync(
        WatchFolderDaemonScope scope,
        bool startAtStartup,
        CancellationToken cancellationToken)
    {
        string enableCommand = startAtStartup ? "enable" : "disable";
        CommandResult primaryResult = await RunSystemctlAsync(scope, $"{enableCommand} {UnitName}", cancellationToken);
        if (!primaryResult.IsSuccess)
        {
            return primaryResult;
        }

        CommandResult legacyDisableResult = await RunSystemctlAsync(scope, $"disable {LegacyUnitName}", cancellationToken);
        if (!legacyDisableResult.IsSuccess && !CanIgnoreUnitMissingOrInactiveError(legacyDisableResult.Output))
        {
            return legacyDisableResult;
        }

        return new CommandResult(true, string.Empty);
    }

    private static async Task StopLegacyUnitIfPresentAsync(
        WatchFolderDaemonScope scope,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(GetUnitFilePath(scope, LegacyUnitName)))
        {
            return;
        }

        CommandResult stopLegacyResult = await RunSystemctlAsync(scope, $"stop {LegacyUnitName}", cancellationToken);
        if (!stopLegacyResult.IsSuccess && !CanIgnoreUnitMissingOrInactiveError(stopLegacyResult.Output))
        {
            return;
        }
    }

    private static async Task<bool> IsUnitActiveAsync(
        WatchFolderDaemonScope scope,
        string unitName,
        CancellationToken cancellationToken)
    {
        CommandResult activeResult = await RunSystemctlAsync(scope, $"is-active {unitName}", cancellationToken);
        return activeResult.Output.Trim().Equals("active", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> IsUnitEnabledAsync(
        WatchFolderDaemonScope scope,
        string unitName,
        CancellationToken cancellationToken)
    {
        CommandResult enabledResult = await RunSystemctlAsync(scope, $"is-enabled {unitName}", cancellationToken);
        return enabledResult.Output.Trim().Equals("enabled", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanIgnoreUnitMissingOrInactiveError(string output)
    {
        return output.Contains("not loaded", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
    }
}
