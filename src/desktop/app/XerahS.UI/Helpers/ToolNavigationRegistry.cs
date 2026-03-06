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

using System;
using System.Collections.Generic;
using XerahS.Core;

namespace XerahS.UI.Helpers;

internal enum ToolNavigationDispatchMode
{
    DirectToolService,
    ExecuteWorkflow
}

internal readonly record struct ToolNavigationRoute(WorkflowType WorkflowType, ToolNavigationDispatchMode DispatchMode);

internal static class ToolNavigationRegistry
{
    private static readonly Dictionary<string, ToolNavigationRoute> TagRoutes = new(StringComparer.Ordinal)
    {
        ["Tools_ColorPicker"] = new(WorkflowType.ColorPicker, ToolNavigationDispatchMode.DirectToolService),
        ["Tools_ScreenColorPicker"] = new(WorkflowType.ScreenColorPicker, ToolNavigationDispatchMode.DirectToolService),
        ["Tools_QrGenerator"] = new(WorkflowType.QRCode, ToolNavigationDispatchMode.DirectToolService),
        ["Tools_QrScanScreen"] = new(WorkflowType.QRCodeDecodeFromScreen, ToolNavigationDispatchMode.DirectToolService),
        ["Tools_QrScanRegion"] = new(WorkflowType.QRCodeScanRegion, ToolNavigationDispatchMode.DirectToolService),
        ["Tools_ImageCombiner"] = new(WorkflowType.ImageCombiner, ToolNavigationDispatchMode.DirectToolService),
        ["Tools_ImageSplitter"] = new(WorkflowType.ImageSplitter, ToolNavigationDispatchMode.DirectToolService),
        ["Tools_ImageThumbnailer"] = new(WorkflowType.ImageThumbnailer, ToolNavigationDispatchMode.DirectToolService),
        ["Tools_VideoEditor"] = new(WorkflowType.VideoEditor, ToolNavigationDispatchMode.ExecuteWorkflow),
        ["Tools_VideoConverter"] = new(WorkflowType.VideoConverter, ToolNavigationDispatchMode.DirectToolService),
        ["Tools_VideoThumbnailer"] = new(WorkflowType.VideoThumbnailer, ToolNavigationDispatchMode.DirectToolService),
        ["Tools_AnalyzeImage"] = new(WorkflowType.AnalyzeImage, ToolNavigationDispatchMode.DirectToolService),
        ["Tools_Ruler"] = new(WorkflowType.Ruler, ToolNavigationDispatchMode.DirectToolService),
        ["Tools_MonitorTest"] = new(WorkflowType.MonitorTest, ToolNavigationDispatchMode.DirectToolService),
        ["Tools_PinToScreenFromScreen"] = new(WorkflowType.PinToScreenFromScreen, ToolNavigationDispatchMode.ExecuteWorkflow),
        ["Tools_PinToScreenFromClipboard"] = new(WorkflowType.PinToScreenFromClipboard, ToolNavigationDispatchMode.ExecuteWorkflow),
        ["Tools_PinToScreenFromFile"] = new(WorkflowType.PinToScreenFromFile, ToolNavigationDispatchMode.ExecuteWorkflow),
        ["Tools_PinToScreenCloseAll"] = new(WorkflowType.PinToScreenCloseAll, ToolNavigationDispatchMode.ExecuteWorkflow),
        ["Tools_OCR"] = new(WorkflowType.OCR, ToolNavigationDispatchMode.ExecuteWorkflow),
        ["Tools_HashCheck"] = new(WorkflowType.HashCheck, ToolNavigationDispatchMode.ExecuteWorkflow),
        ["Tools_ClipboardViewer"] = new(WorkflowType.ClipboardViewer, ToolNavigationDispatchMode.ExecuteWorkflow)
    };

    public static bool TryResolve(string tag, out ToolNavigationRoute route)
    {
        return TagRoutes.TryGetValue(tag, out route);
    }
}
