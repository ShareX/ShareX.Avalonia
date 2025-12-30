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
using System.Collections.Generic;

namespace ShareX.Avalonia.Core;

/// <summary>
/// Hotkey configuration bound to a specific task
/// </summary>
public class HotkeySettings
{
    public HotkeyInfo HotkeyInfo { get; set; }

    public TaskSettings TaskSettings { get; set; }

    public HotkeySettings()
    {
        HotkeyInfo = new HotkeyInfo();
        TaskSettings = new TaskSettings();
    }

    public HotkeySettings(HotkeyType job, Keys hotkey = Keys.None) : this()
    {
        TaskSettings = new TaskSettings { Job = job };
        HotkeyInfo = new HotkeyInfo(hotkey);
    }

    public override string ToString()
    {
        if (HotkeyInfo != null && TaskSettings != null)
        {
            return $"Hotkey: {HotkeyInfo}, Description: {TaskSettings}, Job: {TaskSettings.Job}";
        }

        return "";
    }
}

/// <summary>
/// Hotkeys configuration storage
/// </summary>
public class HotkeysConfig : SettingsBase<HotkeysConfig>
{
    public List<HotkeySettings> Hotkeys { get; set; } = GetDefaultHotkeyList();

    /// <summary>
    /// Get default hotkey list for ShareX
    /// </summary>
    public static List<HotkeySettings> GetDefaultHotkeyList()
    {
        return new List<HotkeySettings>
        {
            new HotkeySettings(HotkeyType.PrintScreen, Keys.PrintScreen),
            new HotkeySettings(HotkeyType.ActiveWindow, Keys.Alt | Keys.PrintScreen),
            new HotkeySettings(HotkeyType.RectangleRegion, Keys.Control | Keys.PrintScreen),
            new HotkeySettings(HotkeyType.ScreenRecorder, Keys.Shift | Keys.PrintScreen),
            new HotkeySettings(HotkeyType.ScreenRecorderGIF, Keys.Control | Keys.Shift | Keys.PrintScreen),
        };
    }
}
