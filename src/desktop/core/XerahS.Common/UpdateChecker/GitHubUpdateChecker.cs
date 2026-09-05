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

using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace XerahS.Common
{
    public class GitHubUpdateChecker : UpdateChecker
    {
        /// <summary>Options for GitHub API JSON. Explicit DefaultJsonTypeInfoResolver avoids "Reflection-based serialization has been disabled" when app uses trimming/source gen.</summary>
        private static readonly JsonSerializerOptions GitHubJsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            PropertyNameCaseInsensitive = true
        };
        protected enum RuntimePlatform
        {
            Unknown,
            Windows,
            MacOS,
            Linux
        }

        public string Owner { get; protected set; }
        public string Repo { get; protected set; }
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

                if (UpdateReleaseInfo(latestRelease, IsPortable, isBrowserDownloadURL: true))
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

        public virtual Task<string?> GetLatestDownloadURL(bool isBrowserDownloadURL)
        {
            return GetLatestDownloadURL(isBrowserDownloadURL, CancellationToken.None);
        }

        /// <summary>
        /// Resolve the latest release download URL, honouring
        /// <paramref name="cancellationToken"/> on the GitHub API request so
        /// long FFmpeg/update downloads can be aborted during URL discovery.
        /// </summary>
        public virtual async Task<string?> GetLatestDownloadURL(bool isBrowserDownloadURL, CancellationToken cancellationToken)
        {
            try
            {
                GitHubRelease? latestRelease = await GetLatestRelease(IncludePreRelease, cancellationToken);

                if (UpdateReleaseInfo(latestRelease, IsPortable, isBrowserDownloadURL))
                {
                    return DownloadURL;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }

            return null;
        }

        protected Task<List<GitHubRelease>?> GetReleases()
        {
            return GetReleases(CancellationToken.None);
        }

        protected async Task<List<GitHubRelease>?> GetReleases(CancellationToken cancellationToken)
        {
            List<GitHubRelease>? releases = null;

            string response = await DownloadGitHubApiStringAsync(ReleasesURL, cancellationToken);

            if (!string.IsNullOrEmpty(response))
            {
                releases = JsonSerializer.Deserialize<List<GitHubRelease>>(response, GitHubJsonOptions);

                if (releases != null && releases.Count > 0)
                {
                    releases.Sort((x, y) => y.published_at.CompareTo(x.published_at));
                }
            }

            return releases;
        }

        protected Task<GitHubRelease?> GetLatestRelease()
        {
            return GetLatestRelease(CancellationToken.None);
        }

        protected async Task<GitHubRelease?> GetLatestRelease(CancellationToken cancellationToken)
        {
            GitHubRelease? latestRelease = null;

            string response = await DownloadGitHubApiStringAsync(LatestReleaseURL, cancellationToken);

            if (!string.IsNullOrEmpty(response))
            {
                latestRelease = JsonSerializer.Deserialize<GitHubRelease>(response, GitHubJsonOptions);
            }

            return latestRelease;
        }

        protected Task<GitHubRelease?> GetLatestRelease(bool includePreRelease)
        {
            return GetLatestRelease(includePreRelease, CancellationToken.None);
        }

        protected async Task<GitHubRelease?> GetLatestRelease(bool includePreRelease, CancellationToken cancellationToken)
        {
            GitHubRelease? latestRelease = null;

            if (includePreRelease)
            {
                List<GitHubRelease>? releases = await GetReleases(cancellationToken);

                if (releases != null && releases.Count > 0)
                {
                    latestRelease = releases[0];
                }
            }
            else
            {
                try
                {
                    latestRelease = await GetLatestRelease(cancellationToken);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    // No stable release found, fall back to checking all releases
                    DebugHelper.WriteLine($"No stable releases found for {Owner}/{Repo}. Checking for pre-releases...");
                    List<GitHubRelease>? releases = await GetReleases(cancellationToken);

                    if (releases != null && releases.Count > 0)
                    {
                        latestRelease = releases[0];
                        DebugHelper.WriteLine($"Found pre-release: {latestRelease.tag_name} (Only pre-releases available)");
                    }
                }
            }

            return latestRelease;
        }

        /// <summary>
        /// Downloads a string from the GitHub API, adding the recommended Accept header
        /// and providing diagnostic logging for rate-limit (403) responses.
        /// </summary>
        private static Task<string> DownloadGitHubApiStringAsync(string url)
        {
            return DownloadGitHubApiStringAsync(url, CancellationToken.None);
        }

        private static async Task<string> DownloadGitHubApiStringAsync(string url, CancellationToken cancellationToken)
        {
            HttpClient client = HttpClientFactory.Create();

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                // GitHub rate-limit: log remaining quota from headers if available.
                string remaining = response.Headers.TryGetValues("X-RateLimit-Remaining", out var vals)
                    ? string.Join(", ", vals)
                    : "unknown";
                DebugHelper.WriteLine($"GitHub API returned 403 for {url}. Rate-limit remaining: {remaining}");
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
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
                    ? [ $"-win-{archToken}-portable.zip", $"-win-{archToken}.zip" ]
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
                    ? [ $"-linux-{archToken}.tar.gz", $"-linux-{archToken}.AppImage" ]
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

            // Architecture-qualified portable archives must never be treated as generic legacy assets.
            GitHubAsset[] legacyAssets = isPortable
                ? assets.Where(asset => asset?.name != null &&
                    !asset.name.Contains("-win-", StringComparison.OrdinalIgnoreCase) &&
                    !asset.name.Contains("-linux-", StringComparison.OrdinalIgnoreCase) &&
                    !asset.name.Contains("-mac-", StringComparison.OrdinalIgnoreCase) &&
                    !asset.name.Contains("-osx-", StringComparison.OrdinalIgnoreCase)).ToArray()
                : assets;
            return FindAssetBySuffixes(legacyAssets, legacySuffixes);
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
