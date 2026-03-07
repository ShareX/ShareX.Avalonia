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

using System.Collections.Specialized;
using Newtonsoft.Json;
using XerahS.Uploaders;

namespace ShareX.Bitly.Plugin;

public sealed class BitlyUrlShortener : UrlShortener
{
    private const string UrlApi = "https://api-ssl.bitly.com/";
    private const string UrlShorten = UrlApi + "v4/shorten";

    private readonly BitlyConfigModel _config;

    public BitlyUrlShortener(BitlyConfigModel config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override UploadResult ShortenURL(string url)
    {
        UploadResult result = new UploadResult { URL = url };

        if (string.IsNullOrEmpty(url))
        {
            return result;
        }

        if (string.IsNullOrEmpty(_config.AccessToken))
        {
            Errors.Add("Bitly access token is required. Configure OAuth and obtain a token.");
            return result;
        }

        var requestBody = new BitlyShortenRequestBody
        {
            long_url = url,
            domain = string.IsNullOrEmpty(_config.Domain) ? "bit.ly" : _config.Domain
        };
        string json = JsonConvert.SerializeObject(requestBody);

        var headers = new NameValueCollection
        {
            ["Authorization"] = "Bearer " + _config.AccessToken
        };

        result.Response = SendRequest(XerahS.Uploaders.HttpMethod.POST, UrlShorten, json, "application/json", null, headers);

        BitlyShortenResponse? responseData = null;
        if (!string.IsNullOrEmpty(result.Response))
        {
            try
            {
                responseData = JsonConvert.DeserializeObject<BitlyShortenResponse>(result.Response);
            }
            catch
            {
                // Leave responseData null
            }
        }

        if (responseData != null && !string.IsNullOrEmpty(responseData.link))
        {
            result.ShortenedURL = responseData.link;
        }

        return result;
    }

    private class BitlyShortenRequestBody
    {
        public string long_url { get; set; } = string.Empty;
        public string domain { get; set; } = "bit.ly";
    }

    private class BitlyShortenResponse
    {
        public DateTime created_at { get; set; }
        public string id { get; set; } = string.Empty;
        public string link { get; set; } = string.Empty;
        public string long_url { get; set; } = string.Empty;
    }
}
