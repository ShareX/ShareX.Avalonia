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

using Avalonia.Input;
using NUnit.Framework;
using XerahS.Core;
using XerahS.Core.Hotkeys;
using XerahS.Platform.Abstractions;

namespace XerahS.Tests.Hotkeys;

public class WorkflowManagerTests
{
    [Test]
    public void RegisterHotkey_WhenClearedToNone_UnregistersPreviousBinding()
    {
        var service = new FakeHotkeyService();
        using var manager = new WorkflowManager(service);
        var settings = new WorkflowSettings(
            WorkflowType.RectangleRegion,
            new HotkeyInfo(Key.L, KeyModifiers.Control | KeyModifiers.Shift));

        var initialRegistration = manager.RegisterHotkey(settings);
        Assert.That(initialRegistration, Is.True);
        Assert.That(service.IsRegistered(settings.HotkeyInfo), Is.True);

        settings.HotkeyInfo.Key = Key.None;
        settings.HotkeyInfo.Modifiers = KeyModifiers.None;

        var clearedRegistration = manager.RegisterHotkey(settings);

        Assert.Multiple(() =>
        {
            Assert.That(clearedRegistration, Is.False);
            Assert.That(settings.HotkeyInfo.Status, Is.EqualTo(HotkeyStatus.NotConfigured));
            Assert.That(service.IsRegistered(settings.HotkeyInfo), Is.False);
            Assert.That(service.UnregisterCallCount, Is.EqualTo(1));
        });
    }

    private sealed class FakeHotkeyService : IHotkeyService
    {
        private readonly HashSet<ushort> _registeredIds = new();
        private ushort _nextId = 1;

        public int UnregisterCallCount { get; private set; }

        public event EventHandler<HotkeyTriggeredEventArgs>? HotkeyTriggered
        {
            add { }
            remove { }
        }

        public bool IsSuspended { get; set; }

        public bool RegisterHotkey(HotkeyInfo hotkeyInfo)
        {
            if (!hotkeyInfo.IsValid)
            {
                hotkeyInfo.Status = HotkeyStatus.NotConfigured;
                return false;
            }

            if (hotkeyInfo.Id == 0)
            {
                hotkeyInfo.Id = _nextId++;
            }

            hotkeyInfo.Status = HotkeyStatus.Registered;
            _registeredIds.Add(hotkeyInfo.Id);
            return true;
        }

        public bool UnregisterHotkey(HotkeyInfo hotkeyInfo)
        {
            if (hotkeyInfo.Id == 0)
            {
                return false;
            }

            UnregisterCallCount++;
            hotkeyInfo.Status = HotkeyStatus.NotConfigured;
            return _registeredIds.Remove(hotkeyInfo.Id);
        }

        public void UnregisterAll()
        {
            _registeredIds.Clear();
        }

        public bool IsRegistered(HotkeyInfo hotkeyInfo)
        {
            return hotkeyInfo.Id != 0 && _registeredIds.Contains(hotkeyInfo.Id);
        }

        public void Dispose()
        {
        }
    }
}
