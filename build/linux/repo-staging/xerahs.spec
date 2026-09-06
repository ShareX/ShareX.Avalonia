# First-party COPR spec for the published Linux payload.
# Consumes the GitHub release tarball (same layout as the AUR PKGBUILD),
# not the internal rpmbuild staging tarball used by XerahS.Packaging.
#
# Stamped by .ai/skills/publish-release/scripts/prepare-distro-repo-assets.sh.

%global debug_package %{nil}
%global _debuginfo_subpackages 0
%global __brp_strip %{nil}
%global __brp_strip_comment_note %{nil}
%global __brp_strip_lto %{nil}

Name:           xerahs
Version:        @VERSION@
Release:        1%{?dist}
Summary:        XerahS - Cross-platform screen capture tool
License:        GPL-3.0-or-later
URL:            https://github.com/@REPO@
# @TARBALL_ARCH@ is the RID suffix only: x64 or arm64 (not linux-x64).
Source0:        https://github.com/@REPO@/releases/download/v%{version}/XerahS-%{version}-linux-@TARBALL_ARCH@.tar.gz
Source1:        https://raw.githubusercontent.com/@REPO@/v%{version}/build/linux/packaging/99-xerahs-input.rules
Source2:        https://raw.githubusercontent.com/@REPO@/v%{version}/build/linux/packaging/com.xerahs.input.policy
ExclusiveArch:  @RPM_ARCH@
BuildRequires:  desktop-file-utils
BuildRequires:  tar
BuildRequires:  gzip
Suggests:       gnome-shell-extension-appindicator
Recommends:     wl-clipboard
Recommends:     xclip

%description
XerahS is a modern, cross-platform screen capture and sharing tool.

On GNOME, install gnome-shell-extension-appindicator to enable the
system tray icon (Settings > Show tray icon).

This package installs the official self-contained Linux payload from
the GitHub release. It is the COPR counterpart of the
XerahS-%{version}-linux-*.rpm asset already attached to each release.

%prep
%setup -c -n %{name}-%{version}

%install
rm -rf %{buildroot}
mkdir -p %{buildroot}/usr/lib/xerahs
mkdir -p %{buildroot}%{_bindir}
cp -a usr/* %{buildroot}/usr/lib/xerahs/
# Remove any pre-existing symlinks or regular files before creating new symlinks.
rm -f %{buildroot}%{_bindir}/xerahs
rm -f %{buildroot}%{_bindir}/omaxerahs
# Use absolute paths to avoid cp -a symlink-copied artifacts in the staging dir.
ln -s /usr/lib/xerahs/XerahS %{buildroot}%{_bindir}/xerahs
ln -s /usr/lib/xerahs/omaxerahs %{buildroot}%{_bindir}/omaxerahs
chmod 755 %{buildroot}/usr/lib/xerahs/XerahS
if [ -f %{buildroot}/usr/lib/xerahs/xerahs-watchfolder-daemon ]; then
  chmod 755 %{buildroot}/usr/lib/xerahs/xerahs-watchfolder-daemon
fi
if [ -f %{buildroot}/usr/lib/xerahs/xerahs-watchfolder-daemon.exe ]; then
  chmod 755 %{buildroot}/usr/lib/xerahs/xerahs-watchfolder-daemon.exe
fi
if [ -f %{buildroot}/usr/lib/xerahs/omaxerahs ]; then
  chmod 755 %{buildroot}/usr/lib/xerahs/omaxerahs
fi
if [ -f %{buildroot}/usr/lib/xerahs/omaxerahs.exe ]; then
  chmod 755 %{buildroot}/usr/lib/xerahs/omaxerahs.exe
fi

mkdir -p %{buildroot}%{_datadir}/applications
cat > %{buildroot}%{_datadir}/applications/xerahs.desktop <<'DESKTOP'
[Desktop Entry]
Name=XerahS
Comment=Cross-platform screen capture and sharing tool
GenericName=Screen Capture
Exec=/usr/bin/xerahs %U
Icon=xerahs
Terminal=false
Type=Application
Categories=Utility;Graphics;GTK;
Keywords=screenshot;screen;capture;share;upload;
StartupWMClass=xerahs
X-GNOME-UsesNotifications=true
X-KDE-DBUS-Restricted-Interfaces=org.kde.KWin.ScreenShot2
DESKTOP
desktop-file-validate %{buildroot}%{_datadir}/applications/xerahs.desktop

mkdir -p %{buildroot}%{_datadir}/pixmaps
if [ -f %{buildroot}/usr/lib/xerahs/Logo.png ]; then
  cp %{buildroot}/usr/lib/xerahs/Logo.png %{buildroot}%{_datadir}/pixmaps/xerahs.png
elif [ -f %{buildroot}/usr/lib/xerahs/ShareX.iconset/icon_512x512.png ]; then
  cp %{buildroot}/usr/lib/xerahs/ShareX.iconset/icon_512x512.png %{buildroot}%{_datadir}/pixmaps/xerahs.png
elif [ -f %{buildroot}/usr/lib/xerahs/xerahs.png ]; then
  cp %{buildroot}/usr/lib/xerahs/xerahs.png %{buildroot}%{_datadir}/pixmaps/xerahs.png
fi

install -D -m 644 %{SOURCE1} %{buildroot}/usr/lib/udev/rules.d/99-xerahs-input.rules
install -D -m 644 %{SOURCE2} %{buildroot}/usr/share/polkit-1/actions/com.xerahs.input.policy

%post
if ! getent group input >/dev/null 2>&1; then groupadd --system input || true; fi
if command -v udevadm >/dev/null 2>&1; then
  udevadm control --reload-rules || true
  udevadm trigger --subsystem-match=input || true
fi

%postun
if command -v udevadm >/dev/null 2>&1; then
  udevadm control --reload-rules || true
  udevadm trigger --subsystem-match=input || true
fi

%files
%{_bindir}/xerahs
%{_bindir}/omaxerahs
/usr/lib/xerahs
/usr/lib/udev/rules.d/99-xerahs-input.rules
%{_datadir}/applications/xerahs.desktop
%{_datadir}/pixmaps/xerahs.png
%{_datadir}/polkit-1/actions/com.xerahs.input.policy

%changelog
* @CHANGELOG_DATE@ ShareX Team <info@getsharex.com> - %{version}-1
- Official Linux payload from GitHub release v%{version}.
