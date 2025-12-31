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

using Newtonsoft.Json;
using System.Collections.Specialized;

namespace ShareX.Ava.Uploaders.Plugins.ImgurPlugin;

/// <summary>
/// Simplified Imgur uploader - supports anonymous uploads
/// </summary>
public class ImgurUploader : ImageUploader
{
    private readonly ImgurConfigModel _config;

    public ImgurUploader(ImgurConfigModel config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override UploadResult Upload(Stream stream, string fileName)
    {
        var args = new Dictionary<string, string>();
        var headers = new NameValueCollection
        {
            ["Authorization"] = "Client-ID " + _config.ClientId
        };

        ReturnResponseOnError = true;

        string fileFormName = fileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                             fileName.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) 
            ? "video" 
            : "image";

        UploadResult result = SendRequestFile("https://api.imgur.com/3/upload", stream, fileName, fileFormName, args, headers);

        if (!string.IsNullOrEmpty(result.Response))
        {
            try
            {
                var response = JsonConvert.DeserializeObject<ImgurResponse>(result.Response);

                if (response?.success == true && response.status == 200 && response.data != null)
                {
                    var imageData = JsonConvert.DeserializeObject<ImgurImageData>(response.data.ToString() ?? "");

                    if (imageData != null && !string.IsNullOrEmpty(imageData.link))
                    {
                        if (_config.DirectLink)
                        {
                            if (_config.UseGIFV && !string.IsNullOrEmpty(imageData.gifv))
                            {
                                result.URL = imageData.gifv;
                            }
                            else
                            {
                                result.URL = imageData.link.TrimEnd('.');
                            }
                        }
                        else
                        {
                            result.URL = $"https://imgur.com/{imageData.id}";
                        }

                        string thumbnail = _config.ThumbnailType switch
                        {
                            ImgurThumbnailType.Small_Square => "s",
                            ImgurThumbnailType.Big_Square => "b",
                            ImgurThumbnailType.Small_Thumbnail => "t",
                            ImgurThumbnailType.Medium_Thumbnail => "m",
                            ImgurThumbnailType.Large_Thumbnail => "l",
                            ImgurThumbnailType.Huge_Thumbnail => "h",
                            _ => "m"
                        };

                        result.ThumbnailURL = $"https://i.imgur.com/{imageData.id}{thumbnail}.jpg";
                        result.DeletionURL = $"https://imgur.com/delete/{imageData.deletehash}";
                    }
                }
                else if (response != null)
                {
                    Errors.Add($"Imgur upload failed: Status {response.status}");
                }
            }
            catch (Exception ex)
            {
                Errors.Add($"Imgur response parsing failed: {ex.Message}");
            }
        }

        return result;
    }

    private class ImgurResponse
    {
        public object? data { get; set; }
        public bool success { get; set; }
        public int status { get; set; }
    }

    private class ImgurImageData
    {
        public string? id { get; set; }
        public string? link { get; set; }
        public string? gifv { get; set; }
        public string? deletehash { get; set; }
    }
}
