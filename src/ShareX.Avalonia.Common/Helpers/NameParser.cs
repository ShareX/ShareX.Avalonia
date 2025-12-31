#region License Information (GPL v3)

/*
    ShareX.Ava - The Avalonia UI implementation of ShareX
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

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ShareX.Ava.Common
{
    public enum NameParserType
    {
        Default,
        Text, // Allows new line
        FileName,
        FilePath,
        URL // URL path encodes
    }

    /// <summary>
    /// Lightweight placeholder parser used by watermarking and custom uploaders.
    /// Supports a focused subset of ShareX tokens (dimensions, date/time, user/computer info and increment).
    /// </summary>
    public class NameParser
    {
        public NameParserType Type { get; }
        public int MaxNameLength { get; set; }
        public int MaxTitleLength { get; set; }
        public int AutoIncrementNumber { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public string? WindowText { get; set; }
        public string? ProcessName { get; set; }
        public TimeZoneInfo? CustomTimeZone { get; set; }

        public NameParser(NameParserType nameParserType)
        {
            Type = nameParserType;
        }

        public static string Parse(NameParserType type, string pattern)
        {
            return new NameParser(type).Parse(pattern);
        }

        public string Parse(string? pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return string.Empty;
            }

            string result = pattern;

            if (Type == NameParserType.Text)
            {
                result = result.Replace("\\n", Environment.NewLine, StringComparison.Ordinal);
            }

            result = ReplaceCoreTokens(result);
            result = SanitizeByType(result);

            if (MaxNameLength > 0 && result.Length > MaxNameLength)
            {
                result = result[..MaxNameLength];
            }

            return result;
        }

        private string ReplaceCoreTokens(string input)
        {
            StringBuilder sb = new StringBuilder(input);

            if (!string.IsNullOrEmpty(WindowText))
            {
                string text = WindowText;

                if (MaxTitleLength > 0 && text.Length > MaxTitleLength)
                {
                    text = text[..MaxTitleLength];
                }

                sb.Replace("%t", text);
            }

            if (!string.IsNullOrEmpty(ProcessName))
            {
                sb.Replace("%pn", ProcessName);
            }

            sb.Replace("%width", ImageWidth > 0 ? ImageWidth.ToString(CultureInfo.InvariantCulture) : string.Empty);
            sb.Replace("%height", ImageHeight > 0 ? ImageHeight.ToString(CultureInfo.InvariantCulture) : string.Empty);

            DateTime now = CustomTimeZone != null ? TimeZoneInfo.ConvertTime(DateTime.Now, CustomTimeZone) : DateTime.Now;
            sb.Replace("%y", now.Year.ToString(CultureInfo.InvariantCulture));
            sb.Replace("%yy", now.ToString("yy", CultureInfo.InvariantCulture));
            sb.Replace("%mo", now.Month.ToString("00", CultureInfo.InvariantCulture));
            sb.Replace("%mon", CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(now.Month));
            sb.Replace("%mon2", CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(now.Month));
            sb.Replace("%d", now.Day.ToString("00", CultureInfo.InvariantCulture));

            if (sb.ToString().Contains("%pm", StringComparison.Ordinal))
            {
                sb.Replace("%h", ((now.Hour % 12 == 0 ? 12 : now.Hour % 12)).ToString("00", CultureInfo.InvariantCulture));
                sb.Replace("%pm", now.Hour >= 12 ? "PM" : "AM");
            }
            else
            {
                sb.Replace("%h", now.Hour.ToString("00", CultureInfo.InvariantCulture));
            }

            sb.Replace("%mi", now.Minute.ToString("00", CultureInfo.InvariantCulture));
            sb.Replace("%s", now.Second.ToString("00", CultureInfo.InvariantCulture));
            sb.Replace("%ms", now.Millisecond.ToString("000", CultureInfo.InvariantCulture));
            sb.Replace("%unix", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));

            sb.Replace("%un", Environment.UserName);
            sb.Replace("%uln", Environment.UserDomainName);
            sb.Replace("%cn", Environment.MachineName);
            sb.Replace("%n", Environment.NewLine);

            if (sb.ToString().Contains("%i", StringComparison.Ordinal))
            {
                AutoIncrementNumber++;
                sb.Replace("%i", AutoIncrementNumber.ToString("d", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string SanitizeByType(string text)
        {
            switch (Type)
            {
                case NameParserType.FileName:
                    return RemoveCharacters(text, Path.GetInvalidFileNameChars());
                case NameParserType.FilePath:
                    return RemoveCharacters(text, Path.GetInvalidPathChars());
                case NameParserType.URL:
                    return Uri.EscapeDataString(text);
                default:
                    return text;
            }
        }

        private static string RemoveCharacters(string text, char[] invalidChars)
        {
            return new string(text.Where(c => !invalidChars.Contains(c)).ToArray());
        }
    }
}
