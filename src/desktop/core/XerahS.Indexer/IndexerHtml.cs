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

using XerahS.Common;
using System.Net;
using System.Reflection;
using System.Text;

namespace XerahS.Indexer
{
    public class IndexerHtml : Indexer
    {
        private static readonly Lazy<string> _defaultCss = new Lazy<string>(LoadDefaultCss);
        private static string DefaultCss => _defaultCss.Value;

        private static string LoadDefaultCss()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("XerahS.Indexer.IndexerHtml.default.css");
            if (stream == null)
            {
                throw new InvalidOperationException("Embedded resource 'XerahS.Indexer.IndexerHtml.default.css' not found.");
            }
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        protected StringBuilder sbContent = new StringBuilder();
        protected string rootFolderPath = string.Empty;
        private const int IndentSize = 2;
        private const int ContentBaseIndent = 3;

        public IndexerHtml(IndexerSettings indexerSettings) : base(indexerSettings)
        {
        }

        public override string Index(string folderPath)
        {
            sbContent.Clear();
            StringBuilder sbHtmlIndex = new StringBuilder();
            AppendHtmlLine(sbHtmlIndex, 0, "<!DOCTYPE html>");
            AppendHtmlLine(sbHtmlIndex, 0, HtmlHelper.StartTag("html", "", "lang=\"en\""));
            AppendHtmlLine(sbHtmlIndex, 1, HtmlHelper.StartTag("head"));
            AppendHtmlLine(sbHtmlIndex, 2, "<meta charset=\"UTF-8\">");
            AppendHtmlLine(sbHtmlIndex, 2, "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">");
            AppendHtmlLine(sbHtmlIndex, 2, "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            AppendHtmlLine(sbHtmlIndex, 2, HtmlHelper.Tag("title", "Index for " + Path.GetFileName(folderPath)));
            AppendHtmlBlock(sbHtmlIndex, 2, GetCssStyle());
            AppendHtmlLine(sbHtmlIndex, 1, HtmlHelper.EndTag("head"));
            AppendHtmlLine(sbHtmlIndex, 1, HtmlHelper.StartTag("body"));
            AppendHtmlLine(sbHtmlIndex, 2, HtmlHelper.StartTag("div", "", "class=\"container\""));
            AppendHtmlLine(sbHtmlIndex, 3, "<input type=\"checkbox\" id=\"theme-toggle\" class=\"ThemeToggleInput\" aria-label=\"Toggle color theme\">");
            AppendHtmlLine(sbHtmlIndex, 3, "<label for=\"theme-toggle\" class=\"ThemeToggleLabel\"><span class=\"ThemeToggleSwitch\" aria-hidden=\"true\"></span><span class=\"ThemeToggleText\"></span></label>");
            AppendHtmlLine(sbHtmlIndex, 3, HtmlHelper.StartTag("div", "", "class=\"IndexContent\""));

            rootFolderPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folderPath));

            FolderInfo folderInfo = GetFolderInfo(rootFolderPath);
            folderInfo.Update();

            IndexFolder(folderInfo);
            string index = sbContent.ToString().TrimEnd();
            AppendHtmlBlock(sbHtmlIndex, 1, index);
            if (settings.AddFooter)
            {
                AppendHtmlLine(sbHtmlIndex, ContentBaseIndent + 1, HtmlHelper.StartTag("footer") + GetFooter() + HtmlHelper.EndTag("footer"));
            }

            AppendHtmlLine(sbHtmlIndex, 3, HtmlHelper.EndTag("div"));
            AppendHtmlLine(sbHtmlIndex, 2, HtmlHelper.EndTag("div"));
            AppendHtmlLine(sbHtmlIndex, 1, HtmlHelper.EndTag("body"));
            AppendHtmlLine(sbHtmlIndex, 0, HtmlHelper.EndTag("html"));
            return sbHtmlIndex.ToString().Trim();
        }

        protected override void IndexFolder(FolderInfo dir, int level = 0)
        {
            int blockIndent = ContentBaseIndent + (level * 2);
            AppendHtmlLine(sbContent, blockIndent, GetFolderNameRow(dir, level));

            string divClass = level > 0 ? "FolderBorder" : "MainFolderBorder";
            AppendHtmlLine(sbContent, blockIndent, HtmlHelper.StartTag("div", "", $"class=\"{divClass}\""));

            if (dir.Files.Count > 0)
            {
                AppendHtmlLine(sbContent, blockIndent + 1, HtmlHelper.StartTag("ul", "", "class=\"FileList\""));

                foreach (FileInfo fi in dir.Files)
                {
                    AppendHtmlLine(sbContent, blockIndent + 2, GetFileNameRow(fi));
                }

                AppendHtmlLine(sbContent, blockIndent + 1, HtmlHelper.EndTag("ul"));
            }
            else if (dir.Folders.Count == 0)
            {
                AppendHtmlLine(sbContent, blockIndent + 1, HtmlHelper.Tag("p", "Empty folder", "", "class=\"EmptyFolder\""));
            }

            foreach (FolderInfo subdir in dir.Folders)
            {
                IndexFolder(subdir, level + 1);
            }

            AppendHtmlLine(sbContent, blockIndent, HtmlHelper.EndTag("div"));
        }

        private string GetFolderNameRow(FolderInfo dir, int level)
        {
            string folderSummary = GetFolderSummary(dir);
            string folderInfoRow = string.IsNullOrEmpty(folderSummary)
                ? string.Empty
                : " " + HtmlHelper.Tag("span", folderSummary, "", "class=\"FolderInfo\"");

            string pathTitle = GetDisplayPathTitle(dir);
            int heading = (level + 1).Clamp(1, 6);

            return HtmlHelper.StartTag("h" + heading) + WebUtility.HtmlEncode(pathTitle) + folderInfoRow + HtmlHelper.EndTag("h" + heading);
        }

        private string GetFileNameRow(FileInfo fi)
        {
            string fileNameRow = HtmlHelper.StartTag("li", "", "class=\"FileRow\"");
            fileNameRow += HtmlHelper.Tag("span", fi.Name, "", "class=\"FileName\"");

            if (settings.ShowSizeInfo)
            {
                fileNameRow += " " + HtmlHelper.Tag("span", fi.Length.ToSizeString(settings.BinaryUnits), "", "class=\"FileSize\"");
            }

            fileNameRow += HtmlHelper.EndTag("li");

            return fileNameRow;
        }

        private string GetFooter()
        {
            return $"Generated by <a href=\"{Links.XerahSWebsite}\">{AppResources.AppName} Directory Indexer</a> on {DateTime.UtcNow:yyyy-MM-dd 'at' HH:mm:ss 'UTC'}";
        }

        private string GetCssStyle()
        {
            string css = DefaultCss;

            if (settings.UseCustomCSSFile)
            {
                string? cssPath = ResolveCustomCssPath(settings.CustomCSSFilePath);
                if (!string.IsNullOrEmpty(cssPath) && File.Exists(cssPath))
                {
                    try
                    {
                        css = File.ReadAllText(cssPath, Encoding.UTF8);
                    }
                    catch (IOException ex)
                    {
                        DebugHelper.WriteException(ex, $"Failed to load custom CSS: {cssPath}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        DebugHelper.WriteException(ex, $"Access denied loading custom CSS: {cssPath}");
                    }
                }
            }

            return $"<style type=\"text/css\">\r\n{css}\r\n</style>";
        }

        private string GetDisplayPathTitle(FolderInfo dir)
        {
            if (!settings.DisplayPath)
            {
                return GetSafeFolderName(dir);
            }

            if (!settings.DisplayPathLimited || string.IsNullOrEmpty(rootFolderPath))
            {
                return dir.FolderPath;
            }

            string relativePath = Path.GetRelativePath(rootFolderPath, dir.FolderPath);
            if (string.Equals(relativePath, ".", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(relativePath))
            {
                return GetSafeFolderName(dir);
            }

            return relativePath;
        }

        private string GetFolderSummary(FolderInfo dir)
        {
            if (dir.IsEmpty)
            {
                return string.Empty;
            }

            StringBuilder summaryBuilder = new StringBuilder();

            if (settings.ShowSizeInfo)
            {
                summaryBuilder.Append(dir.Size.ToSizeString(settings.BinaryUnits));
                summaryBuilder.Append(' ');
            }

            summaryBuilder.Append('(');

            if (dir.TotalFileCount > 0)
            {
                summaryBuilder.Append(dir.TotalFileCount.ToString("n0"));
                summaryBuilder.Append(" file");
                if (dir.TotalFileCount > 1)
                {
                    summaryBuilder.Append('s');
                }
            }

            if (dir.TotalFolderCount > 0)
            {
                if (dir.TotalFileCount > 0)
                {
                    summaryBuilder.Append(", ");
                }

                summaryBuilder.Append(dir.TotalFolderCount.ToString("n0"));
                summaryBuilder.Append(" folder");
                if (dir.TotalFolderCount > 1)
                {
                    summaryBuilder.Append('s');
                }
            }

            summaryBuilder.Append(')');
            return summaryBuilder.ToString();
        }

        private static string GetSafeFolderName(FolderInfo dir)
        {
            return !string.IsNullOrWhiteSpace(dir.FolderName) ? dir.FolderName : dir.FolderPath;
        }

        private static string? ResolveCustomCssPath(string? configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return null;
            }

            string cssPath = configuredPath.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(cssPath))
            {
                return null;
            }

            if (cssPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase) &&
                Uri.TryCreate(cssPath, UriKind.Absolute, out Uri? fileUri) &&
                fileUri.IsFile)
            {
                return fileUri.LocalPath;
            }

            cssPath = Environment.ExpandEnvironmentVariables(cssPath);

            if (cssPath.StartsWith("~", StringComparison.Ordinal))
            {
                string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(homeDirectory))
                {
                    cssPath = Path.Combine(homeDirectory, cssPath.TrimStart('~', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                }
            }

            try
            {
                return Path.GetFullPath(cssPath);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
            catch (PathTooLongException)
            {
                return null;
            }
            catch (System.Security.SecurityException)
            {
                return null;
            }
        }

        private static void AppendHtmlLine(StringBuilder builder, int indentLevel, string line)
        {
            builder.Append(new string(' ', indentLevel * IndentSize));
            builder.AppendLine(line);
        }

        private static void AppendHtmlBlock(StringBuilder builder, int indentLevel, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                AppendHtmlLine(builder, indentLevel, string.Empty);
                return;
            }

            string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                AppendHtmlLine(builder, indentLevel, line);
            }
        }
    }
}
