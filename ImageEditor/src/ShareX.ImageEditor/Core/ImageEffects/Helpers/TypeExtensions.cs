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

using System.ComponentModel;
using System.Reflection;

namespace ShareX.ImageEditor.Core.ImageEffects.Helpers;

public static class TypeExtensions
{
    /// <summary>
    /// Gets the Description attribute value for an enum value
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        FieldInfo? fi = value.GetType().GetField(value.ToString());

        if (fi != null)
        {
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes.Length > 0)
            {
                return attributes[0].Description;
            }
        }

        return value.ToString();
    }

    /// <summary>
    /// Gets the Description attribute value for a Type
    /// </summary>
    public static string GetDescription(this Type type)
    {
        DescriptionAttribute[] attributes = (DescriptionAttribute[])type.GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (attributes.Length > 0)
        {
            return attributes[0].Description;
        }

        return type.Name;
    }
}

public static class StringExtensions
{
    /// <summary>
    /// Truncates a string to a maximum length with optional suffix
    /// </summary>
    public static string Truncate(this string str, int maxLength, string suffix = "")
    {
        if (string.IsNullOrEmpty(str)) return str;

        if (str.Length <= maxLength) return str;

        int truncateLength = maxLength - suffix.Length;
        if (truncateLength <= 0) return suffix.Substring(0, maxLength);

        return str.Substring(0, truncateLength) + suffix;
    }
}
