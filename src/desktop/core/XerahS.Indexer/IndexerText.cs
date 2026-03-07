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
using System.Text;

namespace XerahS.Indexer
{
    public class IndexerText : Indexer
    {
        protected StringBuilder sbContent = new StringBuilder();
        private List<bool> _isLastStack = new List<bool>();

        public IndexerText(IndexerSettings indexerSettings) : base(indexerSettings)
        {
        }

        public override string Index(string folderPath)
        {
            StringBuilder sbTxtIndex = new StringBuilder();

            FolderInfo folderInfo = GetFolderInfo(folderPath);
            folderInfo.Update();

            // Build tree structure
            BuildTree(folderInfo, true);
            
            string index = sbContent.ToString().Trim();

            sbTxtIndex.AppendLine(index);
            if (settings.AddFooter)
            {
                string footer = GetFooter();
                sbTxtIndex.AppendLine("_".Repeat(footer.Length));
                sbTxtIndex.AppendLine(footer);
            }
            return sbTxtIndex.ToString().Trim();
        }

        protected override void IndexFolder(FolderInfo dir, int level = 0)
        {
            // Required by base class but tree mode uses BuildTree instead
        }

        private void BuildTree(FolderInfo dir, bool isLast)
        {
            // Add current folder line
            sbContent.AppendLine(GetTreeFolderRow(dir, isLast));

            _isLastStack.Add(isLast);

            // Collect all items (folders + files) for tree display
            var items = TreeItem.FromFolder(dir);

            // Process items with tree connectors
            for (int i = 0; i < items.Count; i++)
            {
                bool itemIsLast = (i == items.Count - 1);
                var item = items[i];

                if (item.IsFolder && item.FolderInfo != null)
                {
                    BuildTreeFolder(item.FolderInfo, itemIsLast);
                }
                else if (item.FileInfo != null)
                {
                    BuildTreeFile(item.FileInfo, itemIsLast);
                }
            }

            _isLastStack.RemoveAt(_isLastStack.Count - 1);
        }

        private void BuildTreeFolder(FolderInfo dir, bool isLast)
        {
            // Add folder line with proper tree prefix
            sbContent.AppendLine(GetTreeItemRow(dir.FolderName, isLast, true));

            _isLastStack.Add(isLast);

            // Collect sub-items
            var items = TreeItem.FromFolder(dir);

            // Process sub-items
            for (int i = 0; i < items.Count; i++)
            {
                bool itemIsLast = (i == items.Count - 1);
                var item = items[i];

                if (item.IsFolder && item.FolderInfo != null)
                {
                    BuildTreeFolder(item.FolderInfo, itemIsLast);
                }
                else if (item.FileInfo != null)
                {
                    BuildTreeFile(item.FileInfo, itemIsLast);
                }
            }

            _isLastStack.RemoveAt(_isLastStack.Count - 1);
        }

        private void BuildTreeFile(FileInfo file, bool isLast)
        {
            sbContent.AppendLine(GetTreeItemRow(file.Name, isLast, false, file));
        }

        private string GetTreeFolderRow(FolderInfo dir, bool isLast)
        {
            string name = dir.FolderName;
            
            if (settings.ShowSizeInfo && dir.Size > 0)
            {
                name += string.Format(" [{0}]", dir.Size.ToSizeString(settings.BinaryUnits));
            }
            
            if (_isLastStack.Count == 0)
            {
                // Root folder
                return name;
            }
            
            return GetTreePrefix(isLast) + name;
        }

        private string GetTreeItemRow(string name, bool isLast, bool isFolder, FileInfo? fileInfo = null)
        {
            if (settings.ShowSizeInfo && fileInfo != null)
            {
                name += string.Format(" [{0}]", fileInfo.Length.ToSizeString(settings.BinaryUnits));
            }
            
            return GetTreePrefix(isLast) + name;
        }

        private string GetTreePrefix(bool isLast)
        {
            var sb = new StringBuilder();
            
            // Build prefix based on parent levels
            for (int i = 0; i < _isLastStack.Count; i++)
            {
                if (i == _isLastStack.Count - 1)
                {
                    // Current level connector
                    sb.Append(isLast ? "└── " : "├── ");
                }
                else
                {
                    // Parent level - vertical line or space
                    sb.Append(_isLastStack[i] ? "    " : "│   ");
                }
            }
            
            return sb.ToString();
        }

        private string GetFooter()
        {
            return $"Generated by {AppResources.AppName} Directory Indexer on {DateTime.UtcNow:yyyy-MM-dd 'at' HH:mm:ss 'UTC'}. Latest version can be downloaded from: {Links.XerahSWebsite}";
        }
    }
}
