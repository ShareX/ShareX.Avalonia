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

using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace XerahS.Common
{
    public class GitHubUpdateChecker : UpdateChecker
    {
        protected enum RuntimePlatform
        {
            Unknown,
            Windows,
            MacOS,
            Linux
        }

        public string Owner { get; private set; }
        public string Repo { get; private set; }
        public bool IncludePreRelease { get; set; }
        public bool IsPreRelease { get; protected set; }

        private const string APIURL = "https://api.github.com";

        private string ReleasesURL => $"{APIURL}/repos/{Owner}/{Repo}/releases";
        private string LatestReleaseURL => $"{ReleasesURL}/latest";

        public GitHubUpdateChecker(string owner, string repo)
        {
            Owner = owner;
            Repo = repo;
        }

        public override async Task CheckUpdateAsync()
        {
            try
            {
                string url = IncludePreRelease ? ReleasesURL : LatestReleaseURL;
                DebugHelper.WriteLine($"Checking for updates at: {url}");

                GitHubRelease? latestRelease = await GetLatestRelease(IncludePreRelease);

                if (latestRelease == null)
                {
                    DebugHelper.WriteLine($"No release found for {Owner}/{Repo}");
                    Status = UpdateStatus.UpdateCheckFailed;
                    return;
                }

                DebugHelper.WriteLine($"Found release: {latestRelease.tag_name?.TrimStart('v')} (prerelease: {latestRelease.prerelease})");

                if (UpdateReleaseInfo(latestRelease, IsPortable, IsPortable))
                {
                    RefreshStatus();
                    DebugHelper.WriteLine($"Current: {CurrentVersion?.ToString(3)}, Latest: {LatestVersion?.ToString(3)}, Status: {Status}");
                    return;
                }
                else
                {
                    DebugHelper.WriteLine($"Failed to update release info. Tag: {latestRelease.tag_name}, Assets: {latestRelease.assets?.Length ?? 0}");
                }
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e, $"GitHub update check failed for {Owner}/{Repo}.");
            }

            Status = UpdateStatus.UpdateCheckFailed;
        }

        public virtual async Task<string?> GetLatestDownloadURL(bool isBrowserDownloadURL)
        {
            try
            {
                GitHubRelease? latestRelease = await GetLatestRelease(IncludePreRelease);

                if (UpdateReleaseInfo(latestRelease, IsPortable, isBrowserDownloadURL))
                {
                    return DownloadURL;
                }
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }

            return null;
        }

        protected async Task<List<GitHubRelease>?> GetReleases()
        {
            List<GitHubRelease>? releases = null;

            string response = await WebHelpers.DownloadStringAsync(ReleasesURL);

            if (!string.IsNullOrEmpty(response))
            {
                releases = JsonConvert.DeserializeObject<List<GitHubRelease>>(response);

                if (releases != null && releases.Count > 0)
                {
                    releases.Sort((x, y) => y.published_at.CompareTo(x.published_at));
                }
            }

            return releases;
        }

        protected async Task<GitHubRelease?> GetLatestRelease()
        {
            GitHubRelease? latestRelease = null;

            string response = await WebHelpers.DownloadStringAsync(LatestReleaseURL);

            if (!string.IsNullOrEmpty(response))
            {
                latestRelease = JsonConvert.DeserializeObject<GitHubRelease>(response);
            }

            return latestRelease;
        }

        protected async Task<GitHubRelease?> GetLatestRelease(bool includePreRelease)
        {
            GitHubRelease? latestRelease = null;

            if (includePreRelease)
            {
                List<GitHubRelease>? releases = await GetReleases();

                if (releases != null && releases.Count > 0)
                {
                    latestRelease = releases[0];
                }
            }
            else
            {
                try
                {
                    latestRelease = await GetLatestRelease();
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("404"))
                {
                    // No stable release found, fall back to checking all releases
                    DebugHelper.WriteLine($"No stable releases found for {Owner}/{Repo}. Checking for pre-releases...");
                    List<GitHubRelease>? releases = await GetReleases();

                    if (releases != null && releases.Count > 0)
                    {
                        latestRelease = releases[0];
                        DebugHelper.WriteLine($"Found pre-release: {latestRelease.tag_name} (Only pre-releases available)");
                    }
                }
            }

            return latestRelease;
        }

        protected virtual bool UpdateReleaseInfo(GitHubRelease? release, bool isPortable, bool isBrowserDownloadURL)
        {
            if (release != null && !string.IsNullOrEmpty(release.tag_name) && release.tag_name.Length > 1 && release.tag_name[0] == 'v')
            {
                LatestVersion = new Version(release.tag_name.Substring(1));

                if (release.assets != null && release.assets.Length > 0)
                {
                    RuntimePlatform runtimePlatform = GetRuntimePlatform();
                    string archToken = GetArchitectureToken(GetProcessArchitecture());
                    string[] preferredSuffixes = GetPreferredAssetSuffixes(runtimePlatform, archToken, isPortable);

                    GitHubAsset? asset = FindAssetBySuffixes(release.assets, preferredSuffixes);

                    if (asset == null)
                    {
                        // Keep legacy fallback intentionally platform-scoped to prevent cross-platform picks.
                        asset = FindLegacyFallbackAsset(release.assets, runtimePlatform, isPortable);
                    }

                    if (asset != null)
                    {
                        FileName = asset.name ?? string.Empty;
                        DownloadURL = isBrowserDownloadURL ? asset.browser_download_url : asset.url;
                        IsPreRelease = release.prerelease;
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual Architecture GetProcessArchitecture()
        {
            return RuntimeInformation.ProcessArchitecture;
        }

        protected virtual RuntimePlatform GetRuntimePlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RuntimePlatform.Windows;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return RuntimePlatform.MacOS;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return RuntimePlatform.Linux;
            }

            return RuntimePlatform.Unknown;
        }

        private static string GetArchitectureToken(Architecture architecture)
        {
            return architecture switch
            {
                Architecture.Arm64 => "arm64",
                Architecture.X64 => "x64",
                _ => "x64"
            };
        }

        private static string[] GetPreferredAssetSuffixes(RuntimePlatform runtimePlatform, string archToken, bool isPortable)
        {
            return runtimePlatform switch
            {
                RuntimePlatform.Windows => isPortable
                    ? [ $"-win-{archToken}.zip" ]
                    : [ $"-win-{archToken}.exe" ],
                RuntimePlatform.MacOS =>
                [
                    $"-mac-{archToken}.tar.gz",
                    $"-osx-{archToken}.dmg",
                    $"-osx-{archToken}.zip",
                    $"-mac-{archToken}.zip",
                    $"-macos-{archToken}.zip"
                ],
                RuntimePlatform.Linux => isPortable
                    ? [ $"-linux-{archToken}.tar.gz" ]
                    : [ $"-linux-{archToken}.deb", $"-linux-{archToken}.tar.gz", $"-linux-{archToken}.rpm" ],
                _ => []
            };
        }

        private static GitHubAsset? FindAssetBySuffixes(GitHubAsset[] assets, string[] suffixes)
        {
            foreach (string suffix in suffixes)
            {
                foreach (GitHubAsset? asset in assets)
                {
                    if (asset != null && !string.IsNullOrEmpty(asset.name) && asset.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        return asset;
                    }
                }
            }

            return null;
        }

        private static GitHubAsset? FindLegacyFallbackAsset(GitHubAsset[] assets, RuntimePlatform runtimePlatform, bool isPortable)
        {
            if (runtimePlatform != RuntimePlatform.Windows)
            {
                return null;
            }

            string[] legacySuffixes = isPortable
                ? [ "portable.zip" ]
                : [ "-setup.exe", "setup.exe", ".exe" ];

            return FindAssetBySuffixes(assets, legacySuffixes);
        }

        protected class GitHubRelease
        {
            public string? url { get; set; }
            public string? assets_url { get; set; }
            public string? upload_url { get; set; }
            public string? html_url { get; set; }
            public long id { get; set; }
            public string? node_id { get; set; }
            public string? tag_name { get; set; }
            public string? target_commitish { get; set; }
            public string? name { get; set; }
            public bool draft { get; set; }
            public bool prerelease { get; set; }
            public DateTime created_at { get; set; }
            public DateTime published_at { get; set; }
            public GitHubAsset[]? assets { get; set; }
            public string? tarball_url { get; set; }
            public string? zipball_url { get; set; }
            public string? body { get; set; }
        }

        protected class GitHubAsset
        {
            public string? url { get; set; }
            public long id { get; set; }
            public string? node_id { get; set; }
            public string? name { get; set; }
            public string? label { get; set; }
            public string? content_type { get; set; }
            public string? state { get; set; }
            public long size { get; set; }
            public long download_count { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public string? browser_download_url { get; set; }
        }
    }
}
