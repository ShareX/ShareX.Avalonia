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

namespace XerahS.Indexer
{
    /// <summary>
    /// Result of an indexing operation.
    /// </summary>
    public class IndexResult
    {
        public bool Success { get; set; }
        public string OutputFilePath { get; set; } = string.Empty;
        public long TotalFiles { get; set; }
        public long TotalFolders { get; set; }
        public long TotalBytes { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
