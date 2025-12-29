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
using ShareX.Avalonia.ImageEffects.Helpers;
using System.ComponentModel;
using System.Drawing;
using DrawingPoint = System.Drawing.Point;

namespace ShareX.Avalonia.ImageEffects
{
    [Description("Shadow")]
    public class Shadow : ImageEffect
    {
        private float opacity;
        private int size;

        [DefaultValue(0.6f), Description("Choose a value between 0.1 and 1.0")]
        public float Opacity
        {
            get => opacity;
            set => opacity = value.Clamp(0.1f, 1.0f);
        }

        [DefaultValue(10)]
        public int Size
        {
            get => size;
            set => size = value.Max(0);
        }

        [DefaultValue(0f)]
        public float Darkness { get; set; }

        [DefaultValue(typeof(Color), "Black")]
        public Color Color { get; set; }

        [DefaultValue(typeof(DrawingPoint), "0, 0")]
        public DrawingPoint Offset { get; set; }

        [DefaultValue(true)]
        public bool AutoResize { get; set; }

        public Shadow()
        {
            this.ApplyDefaultPropertyValues();
        }

        public override Bitmap Apply(Bitmap bmp)
        {
            return ImageEffectsProcessing.AddShadow(bmp, Opacity, Size, Darkness + 1, Color, Offset, AutoResize);
        }

        protected override string GetSummary()
        {
            return Size.ToString();
        }
    }
}
