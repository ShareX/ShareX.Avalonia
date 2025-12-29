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

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace ShareX.Avalonia.ImageEffects.Helpers
{
    public class GradientInfo
    {
        [DefaultValue(LinearGradientMode.Vertical)]
        public LinearGradientMode Type { get; set; }

        public List<GradientStop> Colors { get; set; }

        public bool IsValid => Colors != null && Colors.Count > 0;

        public bool IsVisible => IsValid && Colors.Any(x => x.Color.A > 0);

        public bool IsTransparent => IsValid && Colors.Any(x => x.Color.A == 0);

        public GradientInfo()
            : this(LinearGradientMode.Vertical)
        {
        }

        public GradientInfo(LinearGradientMode type)
        {
            Type = type;
            Colors = new List<GradientStop>();
        }

        public GradientInfo(LinearGradientMode type, params GradientStop[] colors)
            : this(type)
        {
            Colors = colors.ToList();
        }

        public GradientInfo(LinearGradientMode type, params Color[] colors)
            : this(type)
        {
            for (int i = 0; i < colors.Length; i++)
            {
                Colors.Add(new GradientStop(colors[i], (int)System.Math.Round(100f / (colors.Length - 1) * i)));
            }
        }

        public GradientInfo(params GradientStop[] colors)
            : this(LinearGradientMode.Vertical, colors)
        {
        }

        public GradientInfo(params Color[] colors)
            : this(LinearGradientMode.Vertical, colors)
        {
        }

        public void Sort()
        {
            Colors.Sort((x, y) => x.Location.CompareTo(y.Location));
        }

        public void Reverse()
        {
            Colors.Reverse();

            foreach (GradientStop color in Colors)
            {
                color.Location = 100 - color.Location;
            }
        }

        public ColorBlend GetColorBlend()
        {
            List<GradientStop> colors = new List<GradientStop>(Colors.OrderBy(x => x.Location));

            if (!colors.Any(x => x.Location == 0))
            {
                colors.Insert(0, new GradientStop(colors[0].Color, 0f));
            }

            if (!colors.Any(x => x.Location == 100))
            {
                colors.Add(new GradientStop(colors[colors.Count - 1].Color, 100f));
            }

            ColorBlend colorBlend = new ColorBlend
            {
                Colors = colors.Select(x => x.Color).ToArray(),
                Positions = colors.Select(x => x.Location / 100).ToArray()
            };

            return colorBlend;
        }

        public LinearGradientBrush GetGradientBrush(Rectangle rect)
        {
            LinearGradientBrush brush = new LinearGradientBrush(rect, Color.Transparent, Color.Transparent, Type)
            {
                InterpolationColors = GetColorBlend()
            };
            return brush;
        }

        public void Draw(Graphics g, Rectangle rect)
        {
            if (!IsValid)
            {
                return;
            }

            using (LinearGradientBrush brush = GetGradientBrush(new Rectangle(0, 0, rect.Width, rect.Height)))
            {
                g.FillRectangle(brush, rect);
            }
        }

        public void Draw(Image img)
        {
            if (!IsValid)
            {
                return;
            }

            using (Graphics g = Graphics.FromImage(img))
            {
                Draw(g, new Rectangle(0, 0, img.Width, img.Height));
            }
        }

        public override string ToString()
        {
            return "Gradient";
        }
    }
}
