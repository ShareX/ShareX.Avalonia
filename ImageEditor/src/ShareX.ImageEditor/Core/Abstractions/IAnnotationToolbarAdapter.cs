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

using ShareX.ImageEditor.Core.Annotations;
using System.ComponentModel;

namespace ShareX.ImageEditor.Core.Abstractions;

/// <summary>
/// Core-facing contract for annotation toolbar state and actions.
/// </summary>
public interface IAnnotationToolbarAdapter : INotifyPropertyChanged
{
    EditorTool ActiveTool { get; set; }
    string StrokeColor { get; set; }
    string FillColor { get; set; }
    string TextColor { get; set; }
    int StrokeWidth { get; set; }
    float FontSize { get; set; }
    float EffectStrength { get; set; }
    bool ShadowEnabled { get; set; }
    bool TextBold { get; set; }
    bool TextItalic { get; set; }
    bool TextUnderline { get; set; }
    bool CanUndo { get; }
    bool CanRedo { get; }
    bool HasSelection { get; }
    bool ShowBorderColor { get; }
    bool ShowFillColor { get; }
    bool ShowTextColor { get; }
    bool ShowThickness { get; }
    bool ShowFontSize { get; }
    bool ShowStrength { get; }
    bool ShowTextStyle { get; }
    bool ShowShadow { get; }
    bool ShowToolOptions { get; }
    void Undo();
    void Redo();
    void DeleteSelection();
    void ClearSelection();
}
