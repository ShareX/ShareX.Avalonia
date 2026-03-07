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

using Avalonia.Controls;

namespace ShareX.ImageEditor.Core.Annotations;

public partial class TextAnnotation
{
    /// <summary>
    /// Creates the Avalonia visual for this annotation (TextBox for editing)
    /// </summary>
    public Control CreateVisual()
    {
        var control = new ShareX.ImageEditor.Presentation.Controls.OutlinedTextControl
        {
            Annotation = this,
            Tag = this,
            IsHitTestVisible = false
        };

        if (ShadowEnabled)
        {
            control.Effect = new Avalonia.Media.DropShadowEffect
            {
                OffsetX = 3,
                OffsetY = 3,
                BlurRadius = 4,
                Color = Avalonia.Media.Color.FromArgb(128, 0, 0, 0)
            };
        }

        // Rotation is handled by AnnotationVisualFactory.UpdateVisualControl

        return control;
    }
}
