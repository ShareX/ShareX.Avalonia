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

            if (sb.ToString().Contains("%i", StringComparison.Ordinal)
                || sb.ToString().Contains("%ib", StringComparison.Ordinal)
                || sb.ToString().Contains("%iB", StringComparison.Ordinal)
                || sb.ToString().Contains("%iAa", StringComparison.Ordinal)
                || sb.ToString().Contains("%iaA", StringComparison.Ordinal)
                || sb.ToString().Contains("%ia", StringComparison.Ordinal)
                || sb.ToString().Contains("%iA", StringComparison.Ordinal)
                || sb.ToString().Contains("%ix", StringComparison.Ordinal)
                || sb.ToString().Contains("%iX", StringComparison.Ordinal))
            {
                AutoIncrementNumber++;

                // Base
                try
                {
                    foreach (Tuple<string, int[]> entry in ListEntryWithValues(sb.ToString(), "%ib", 2))
                    {
                        sb.Replace(entry.Item1, GeneralHelpers.AddZeroes(AutoIncrementNumber.ToBase(entry.Item2[0], GeneralHelpers.AlphanumericInverse), entry.Item2[1]));
                    }
                    foreach (Tuple<string, int[]> entry in ListEntryWithValues(sb.ToString(), "%iB", 2))
                    {
                        sb.Replace(entry.Item1, GeneralHelpers.AddZeroes(AutoIncrementNumber.ToBase(entry.Item2[0], GeneralHelpers.Alphanumeric), entry.Item2[1]));
                    }
                }
                catch
                {
                }

                // Alphanumeric Dual Case (Base 62)
                foreach (Tuple<string, int> entry in ListEntryWithValue(sb.ToString(), "%iAa"))
                {
                    sb.Replace(entry.Item1, GeneralHelpers.AddZeroes(AutoIncrementNumber.ToBase(62, GeneralHelpers.Alphanumeric), entry.Item2));
                }
                sb.Replace("%iAa", AutoIncrementNumber.ToBase(62, GeneralHelpers.Alphanumeric));

                // Alphanumeric Dual Case (Base 62)
                foreach (Tuple<string, int> entry in ListEntryWithValue(sb.ToString(), "%iaA"))
                {
                    sb.Replace(entry.Item1, GeneralHelpers.AddZeroes(AutoIncrementNumber.ToBase(62, GeneralHelpers.AlphanumericInverse), entry.Item2));
                }
                sb.Replace("%iaA", AutoIncrementNumber.ToBase(62, GeneralHelpers.AlphanumericInverse));

                // Alphanumeric Single Case (Base 36)
                foreach (Tuple<string, int> entry in ListEntryWithValue(sb.ToString(), "%ia"))
                {
                    sb.Replace(entry.Item1, GeneralHelpers.AddZeroes(AutoIncrementNumber.ToBase(36, GeneralHelpers.Alphanumeric), entry.Item2).ToLowerInvariant());
                }
                sb.Replace("%ia", AutoIncrementNumber.ToBase(36, GeneralHelpers.Alphanumeric).ToLowerInvariant());

                // Alphanumeric Single Case Capital (Base 36)
                foreach (Tuple<string, int> entry in ListEntryWithValue(sb.ToString(), "%iA"))
                {
                    sb.Replace(entry.Item1, GeneralHelpers.AddZeroes(AutoIncrementNumber.ToBase(36, GeneralHelpers.Alphanumeric), entry.Item2).ToUpperInvariant());
                }
                sb.Replace("%iA", AutoIncrementNumber.ToBase(36, GeneralHelpers.Alphanumeric).ToUpperInvariant());

                // Hexadecimal (Base 16)
                foreach (Tuple<string, int> entry in ListEntryWithValue(sb.ToString(), "%ix"))
                {
                    sb.Replace(entry.Item1, AutoIncrementNumber.ToString("x" + entry.Item2.ToString()));
                }
                sb.Replace("%ix", AutoIncrementNumber.ToString("x"));

                // Hexadecimal Capital (Base 16)
                foreach (Tuple<string, int> entry in ListEntryWithValue(sb.ToString(), "%iX"))
                {
                    sb.Replace(entry.Item1, AutoIncrementNumber.ToString("X" + entry.Item2.ToString()));
                }
                sb.Replace("%iX", AutoIncrementNumber.ToString("X"));

                // Number (Base 10)
                foreach (Tuple<string, int> entry in ListEntryWithValue(sb.ToString(), "%i"))
                {
                    sb.Replace(entry.Item1, AutoIncrementNumber.ToString("d" + entry.Item2.ToString()));
                }
                sb.Replace("%i", AutoIncrementNumber.ToString("d"));
            }
            
            string result = sb.ToString();

            // Random generators
            foreach (Tuple<string, int> entry in ListEntryWithValue(result, "%rna"))
            {
                result = result.Replace(entry.Item1, GeneralHelpers.RepeatGenerator(entry.Item2, () => GeneralHelpers.GetRandomChar(GeneralHelpers.Base56).ToString()));
            }

            foreach (Tuple<string, int> entry in ListEntryWithValue(result, "%rn"))
            {
                result = result.Replace(entry.Item1, GeneralHelpers.RepeatGenerator(entry.Item2, () => GeneralHelpers.GetRandomChar(GeneralHelpers.Numbers).ToString()));
            }

            foreach (Tuple<string, int> entry in ListEntryWithValue(result, "%ra"))
            {
                result = result.Replace(entry.Item1, GeneralHelpers.RepeatGenerator(entry.Item2, () => GeneralHelpers.GetRandomChar(GeneralHelpers.Alphanumeric).ToString()));
            }

            foreach (Tuple<string, int> entry in ListEntryWithValue(result, "%rx"))
            {
                result = result.Replace(entry.Item1, GeneralHelpers.RepeatGenerator(entry.Item2, () => GeneralHelpers.GetRandomChar(GeneralHelpers.Hexadecimal.ToLowerInvariant()).ToString()));
            }
            
            foreach (Tuple<string, int> entry in ListEntryWithValue(result, "%rX"))
            {
                result = result.Replace(entry.Item1, GeneralHelpers.RepeatGenerator(entry.Item2, () => GeneralHelpers.GetRandomChar(GeneralHelpers.Hexadecimal.ToUpperInvariant()).ToString()));
            }
            
            // Default random replacements (single char if no argument, though logical default is usually length 1 or ignored? Original logic doesn't explicitly handle no-arg %ra other than as a single replacement in some contexts, but here let's stick to explicit replacements if needed. 
            // Actually original code handles %rna, %rn etc without arguments as single char too.
            
            result = result.Replace("%rna", GeneralHelpers.GetRandomChar(GeneralHelpers.Base56).ToString());
            result = result.Replace("%rn", GeneralHelpers.GetRandomChar(GeneralHelpers.Numbers).ToString());
            result = result.Replace("%ra", GeneralHelpers.GetRandomChar(GeneralHelpers.Alphanumeric).ToString());
            result = result.Replace("%rx", GeneralHelpers.GetRandomChar(GeneralHelpers.Hexadecimal.ToLowerInvariant()).ToString());
            result = result.Replace("%rX", GeneralHelpers.GetRandomChar(GeneralHelpers.Hexadecimal.ToUpperInvariant()).ToString());
            result = result.Replace("%guid", Guid.NewGuid().ToString().ToLowerInvariant());
            result = result.Replace("%GUID", Guid.NewGuid().ToString().ToUpperInvariant());

            return result;
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

        private IEnumerable<Tuple<string, string[]>> ListEntryWithArguments(string text, string entry, int elements)
        {
            int index = 0;
            while ((index = text.IndexOf(entry + "{", index, StringComparison.Ordinal)) != -1)
            {
                int closeIndex = text.IndexOf("}", index, StringComparison.Ordinal);
                if (closeIndex != -1)
                {
                    string fullMatch = text.Substring(index, closeIndex - index + 1);
                    string content = text.Substring(index + entry.Length + 1, closeIndex - (index + entry.Length + 1));
                    string[] args = content.Split(',');
                    
                    if (elements > args.Length)
                    {
                        Array.Resize(ref args, elements);
                    }
                    
                    yield return new Tuple<string, string[]>(fullMatch, args);
                    
                    index = closeIndex + 1;
                }
                else
                {
                    break;
                }
            }
        }

        private IEnumerable<Tuple<string, string>> ListEntryWithArgument(string text, string entry)
        {
            foreach (Tuple<string, string[]> o in ListEntryWithArguments(text, entry, 1))
            {
                yield return new Tuple<string, string>(o.Item1, o.Item2[0]);
            }
        }

        private IEnumerable<Tuple<string, int[]>> ListEntryWithValues(string text, string entry, int elements)
        {
            foreach (Tuple<string, string[]> o in ListEntryWithArguments(text, entry, elements))
            {
                int[] a = new int[o.Item2.Length];
                for (int i = 0; i < o.Item2.Length; ++i)
                {
                    if (int.TryParse(o.Item2[i], out int n))
                    {
                        a[i] = n;
                    }
                }
                yield return new Tuple<string, int[]>(o.Item1, a);
            }
        }

        private IEnumerable<Tuple<string, int>> ListEntryWithValue(string text, string entry)
        {
            foreach (Tuple<string, int[]> o in ListEntryWithValues(text, entry, 1))
            {
                yield return new Tuple<string, int>(o.Item1, o.Item2[0]);
            }
        }
    }
}
