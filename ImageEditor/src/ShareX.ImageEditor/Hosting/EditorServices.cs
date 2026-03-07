#region License Information (GPL v3)

/*
    ShareX.ImageEditor - The UI-agnostic Editor library for ShareX
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

using ShareX.ImageEditor.Hosting.Diagnostics;

namespace ShareX.ImageEditor.Hosting;

/// <summary>
/// Service locator for Editor services that must be provided by the host application.
/// </summary>
public static class EditorServices
{
    /// <summary>
    /// Clipboard service for copy/paste operations.
    /// Host applications should set this before using clipboard functionality.
    /// </summary>
    public static IClipboardService? Clipboard { get; set; }

    /// <summary>
    /// Optional diagnostics sink for exception/messages emitted by ImageEditor.
    /// </summary>
    public static IEditorDiagnosticsSink? Diagnostics { get; set; }

    public static void ReportInformation(string source, string message)
    {
        ReportDiagnostic(EditorDiagnosticLevel.Information, source, message, null);
    }

    public static void ReportWarning(string source, string message, Exception? exception = null)
    {
        ReportDiagnostic(EditorDiagnosticLevel.Warning, source, message, exception);
    }

    public static void ReportError(string source, string message, Exception? exception = null)
    {
        ReportDiagnostic(EditorDiagnosticLevel.Error, source, message, exception);
    }

    public static void ReportDiagnostic(EditorDiagnosticLevel level, string source, string message, Exception? exception = null)
    {
        IEditorDiagnosticsSink? sink = Diagnostics;
        if (sink == null)
        {
            return;
        }

        var diagnosticEvent = new EditorDiagnosticEvent(level, source, message, exception);

        try
        {
            sink.Report(diagnosticEvent);
        }
        catch
        {
            // Diagnostics must never break editor functionality.
        }
    }
}
