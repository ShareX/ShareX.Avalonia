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
using NUnit.Framework;
using XerahS.Common;

namespace XerahS.Tests.Helpers;

[TestFixture]
public class GitHubUpdateCheckerAssetSelectionTests
{
    private static readonly string[] AllReleaseAssets =
    [
        "XerahS-1.2.3-win-x64.exe",
        "XerahS-1.2.3-win-arm64.exe",
        "XerahS-1.2.3-mac-x64.tar.gz",
        "XerahS-1.2.3-mac-arm64.tar.gz",
        "XerahS-1.2.3-linux-x64.tar.gz",
        "XerahS-1.2.3-linux-x64.deb",
        "XerahS-1.2.3-linux-x64.rpm",
        "XerahS-1.2.3-linux-arm64.tar.gz",
        "XerahS-1.2.3-linux-arm64.deb",
        "XerahS-1.2.3-linux-arm64.rpm"
    ];

    [TestCase("windows", Architecture.X64, "XerahS-1.2.3-win-x64.exe")]
    [TestCase("windows", Architecture.Arm64, "XerahS-1.2.3-win-arm64.exe")]
    [TestCase("linux", Architecture.X64, "XerahS-1.2.3-linux-x64.deb")]
    [TestCase("linux", Architecture.Arm64, "XerahS-1.2.3-linux-arm64.deb")]
    [TestCase("macos", Architecture.X64, "XerahS-1.2.3-mac-x64.tar.gz")]
    [TestCase("macos", Architecture.Arm64, "XerahS-1.2.3-mac-arm64.tar.gz")]
    public void UpdateReleaseInfo_SelectsAssetForEachRuntimeCombination(string runtimePlatform, Architecture architecture, string expectedAsset)
    {
        var checker = new TestGitHubUpdateChecker(runtimePlatform, architecture);

        bool resolved = checker.TryResolveAssets(AllReleaseAssets, isPortable: false);

        Assert.That(resolved, Is.True);
        Assert.That(checker.FileName, Is.EqualTo(expectedAsset));
    }

    [Test]
    public void UpdateReleaseInfo_OnMacOS_DoesNotFallbackToWindowsExeWhenMacAssetsMissing()
    {
        var checker = new TestGitHubUpdateChecker("macos", Architecture.Arm64);
        string[] windowsOnlyAssets =
        [
            "XerahS-1.2.3-win-x64.exe",
            "XerahS-1.2.3-win-arm64.exe"
        ];

        bool resolved = checker.TryResolveAssets(windowsOnlyAssets, isPortable: false);

        Assert.That(resolved, Is.False);
        Assert.That(checker.DownloadURL, Is.Null);
    }

    [Test]
    public void UpdateReleaseInfo_OnWindows_UsesLegacySetupFallback()
    {
        var checker = new TestGitHubUpdateChecker("windows", Architecture.X64);
        string[] legacyAssets =
        [
            "XerahS-setup.exe"
        ];

        bool resolved = checker.TryResolveAssets(legacyAssets, isPortable: false);

        Assert.That(resolved, Is.True);
        Assert.That(checker.FileName, Is.EqualTo("XerahS-setup.exe"));
    }

    [Test]
    public void UpdateReleaseInfo_OnLinuxPortable_PrefersTarball()
    {
        var checker = new TestGitHubUpdateChecker("linux", Architecture.Arm64);
        string[] assets =
        [
            "XerahS-1.2.3-linux-arm64.deb",
            "XerahS-1.2.3-linux-arm64.tar.gz"
        ];

        bool resolved = checker.TryResolveAssets(assets, isPortable: true);

        Assert.That(resolved, Is.True);
        Assert.That(checker.FileName, Is.EqualTo("XerahS-1.2.3-linux-arm64.tar.gz"));
    }

    private sealed class TestGitHubUpdateChecker(string runtimePlatform, Architecture architecture) : GitHubUpdateChecker("ShareX", "XerahS")
    {
        private readonly string _runtimePlatform = runtimePlatform;
        private readonly Architecture _architecture = architecture;

        protected override Architecture GetProcessArchitecture()
        {
            return _architecture;
        }

        protected override RuntimePlatform GetRuntimePlatform()
        {
            return _runtimePlatform.ToLowerInvariant() switch
            {
                "windows" => RuntimePlatform.Windows,
                "linux" => RuntimePlatform.Linux,
                "macos" => RuntimePlatform.MacOS,
                _ => RuntimePlatform.Unknown
            };
        }

        public bool TryResolveAssets(string[] assetNames, bool isPortable)
        {
            GitHubAsset[] assets = new GitHubAsset[assetNames.Length];

            for (int i = 0; i < assetNames.Length; i++)
            {
                string assetName = assetNames[i];
                assets[i] = new GitHubAsset
                {
                    name = assetName,
                    browser_download_url = $"https://example.invalid/{assetName}",
                    url = $"https://api.example.invalid/{assetName}"
                };
            }

            GitHubRelease release = new GitHubRelease
            {
                tag_name = "v1.2.3",
                assets = assets
            };

            return UpdateReleaseInfo(release, isPortable, isBrowserDownloadURL: true);
        }
    }
}
