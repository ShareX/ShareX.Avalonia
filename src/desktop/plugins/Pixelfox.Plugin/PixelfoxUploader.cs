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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using XerahS.Common;
using XerahS.Uploaders;

namespace ShareX.Pixelfox.Plugin;

public sealed class PixelfoxUploader : ImageUploader
{
    private static readonly HttpClient HttpClient = new();

    private readonly PixelfoxConfigModel _config;
    private readonly string _apiKey;

    public PixelfoxUploader(PixelfoxConfigModel config, string apiKey)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _apiKey = apiKey ?? string.Empty;
    }

    public override UploadResult Upload(Stream stream, string fileName)
    {
        UploadResult result = new();

        string normalizedServerUrl = NormalizeServerUrl(_config.ServerUrl);
        if (string.IsNullOrWhiteSpace(normalizedServerUrl))
        {
            Errors.Add("Pixelfox server URL is required.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            Errors.Add("Pixelfox API key is required.");
            return result;
        }

        if (!TryParseAlbumId(_config.AlbumId, out long? albumId, out string? albumError))
        {
            Errors.Add(albumError ?? "Pixelfox album ID is invalid.");
            return result;
        }

        try
        {
            MemoryStream workingCopy = PrepareSeekableStream(stream);
            using (workingCopy)
            {
                workingCopy.Position = 0;

                PixelfoxUploadSession session = CreateUploadSessionAsync(normalizedServerUrl, workingCopy.Length, albumId).GetAwaiter().GetResult();

                if (workingCopy.Length > session.MaxBytes)
                {
                    Errors.Add($"Pixelfox session limit exceeded. File size {workingCopy.Length} bytes, max {session.MaxBytes} bytes.");
                    return result;
                }

                ProgressManager progress = new(workingCopy.Length);
                workingCopy.Position = 0;
                PixelfoxStorageUploadResponse acceptedUpload = UploadFileAsync(session, workingCopy, fileName, progress).GetAwaiter().GetResult();

                PixelfoxProcessingStatus status = WaitForProcessingAsync(normalizedServerUrl, acceptedUpload.ImageUuid).GetAwaiter().GetResult();
                if (status.Failed)
                {
                    Errors.Add("Pixelfox reported that image processing failed.");
                    return result;
                }

                PixelfoxImageResource image = GetImageAsync(normalizedServerUrl, acceptedUpload.ImageUuid).GetAwaiter().GetResult();
                string? finalUrl = ResolveBestUrl(image, normalizedServerUrl);

                result.Response = JsonConvert.SerializeObject(image, Formatting.Indented);
                result.URL = finalUrl;
                result.IsSuccess = !string.IsNullOrWhiteSpace(finalUrl);

                if (string.IsNullOrWhiteSpace(finalUrl))
                {
                    Errors.Add("Pixelfox upload succeeded but no final image URL was returned.");
                }

                return result;
            }
        }
        catch (Exception ex)
        {
            Errors.Add(ex.Message);
            return result;
        }
    }

    public static string NormalizeServerUrl(string? serverUrl)
    {
        string normalized = (serverUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        normalized = normalized.TrimEnd('/');
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri))
        {
            return string.Empty;
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    private async Task<PixelfoxUploadSession> CreateUploadSessionAsync(string serverUrl, long fileSize, long? albumId)
    {
        var payload = new JObject
        {
            ["file_size"] = fileSize,
            ["is_nsfw"] = _config.IsNsfw,
            ["processing"] = new JObject
            {
                ["profile"] = _config.ProcessingProfile == PixelfoxProcessingProfile.OriginalOnly
                    ? "original_only"
                    : "default"
            }
        };

        if (albumId.HasValue)
        {
            payload["album_id"] = albumId.Value;
        }

        using HttpRequestMessage request = new(System.Net.Http.HttpMethod.Post, $"{serverUrl}/api/v1/upload/sessions")
        {
            Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json")
        };
        AddApiKeyHeaders(request);

        using HttpResponseMessage response = await HttpClient.SendAsync(request);
        string responseText = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, responseText);

        PixelfoxUploadSession? session = JsonConvert.DeserializeObject<PixelfoxUploadSession>(responseText);
        if (session == null || string.IsNullOrWhiteSpace(session.UploadUrl) || string.IsNullOrWhiteSpace(session.Token))
        {
            throw new InvalidOperationException("Pixelfox returned an invalid upload session.");
        }

        return session;
    }

    private async Task<PixelfoxStorageUploadResponse> UploadFileAsync(
        PixelfoxUploadSession session,
        Stream stream,
        string fileName,
        ProgressManager progress)
    {
        using HttpRequestMessage request = new(System.Net.Http.HttpMethod.Post, session.UploadUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        using MultipartFormDataContent form = new();
        using StreamContent fileContent = new(new ProgressReadStream(stream, bytesTransferred =>
        {
            if (AllowReportProgress && progress.UpdateProgress(bytesTransferred))
            {
                OnProgressChanged(progress);
            }
        }));

        fileContent.Headers.ContentType = new MediaTypeHeaderValue(MimeTypes.GetMimeTypeFromFileName(fileName));
        form.Add(fileContent, "file", fileName);
        request.Content = form;

        using HttpResponseMessage response = await HttpClient.SendAsync(request);
        string responseText = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, responseText);

        PixelfoxStorageUploadResponse? acceptedUpload = JsonConvert.DeserializeObject<PixelfoxStorageUploadResponse>(responseText);
        if (acceptedUpload == null || string.IsNullOrWhiteSpace(acceptedUpload.ImageUuid))
        {
            throw new InvalidOperationException("Pixelfox did not return an image UUID for the uploaded file.");
        }

        return acceptedUpload;
    }

    private async Task<PixelfoxProcessingStatus> WaitForProcessingAsync(string serverUrl, string imageUuid)
    {
        DateTime deadline = DateTime.UtcNow.AddMinutes(2);

        while (DateTime.UtcNow < deadline)
        {
            PixelfoxProcessingStatus status = await GetStatusAsync(serverUrl, imageUuid);
            if (status.Complete || status.Failed)
            {
                return status;
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException("Timed out while waiting for Pixelfox to finish processing the upload.");
    }

    private async Task<PixelfoxProcessingStatus> GetStatusAsync(string serverUrl, string imageUuid)
    {
        using HttpRequestMessage request = new(System.Net.Http.HttpMethod.Get, $"{serverUrl}/api/v1/images/{Uri.EscapeDataString(imageUuid)}/status");
        AddApiKeyHeaders(request);

        using HttpResponseMessage response = await HttpClient.SendAsync(request);
        string responseText = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, responseText);

        PixelfoxProcessingStatus? status = JsonConvert.DeserializeObject<PixelfoxProcessingStatus>(responseText);
        if (status == null)
        {
            throw new InvalidOperationException("Pixelfox returned an invalid processing status response.");
        }

        return status;
    }

    private async Task<PixelfoxImageResource> GetImageAsync(string serverUrl, string imageUuid)
    {
        using HttpRequestMessage request = new(System.Net.Http.HttpMethod.Get, $"{serverUrl}/api/v1/images/{Uri.EscapeDataString(imageUuid)}");
        AddApiKeyHeaders(request);

        using HttpResponseMessage response = await HttpClient.SendAsync(request);
        string responseText = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, responseText);

        PixelfoxImageResource? image = JsonConvert.DeserializeObject<PixelfoxImageResource>(responseText);
        if (image == null)
        {
            throw new InvalidOperationException("Pixelfox returned an invalid image resource response.");
        }

        return image;
    }

    private static string? ResolveBestUrl(PixelfoxImageResource image, string serverUrl)
    {
        if (!string.IsNullOrWhiteSpace(image.ViewUrl))
        {
            return image.ViewUrl.StartsWith("/", StringComparison.Ordinal)
                ? serverUrl + image.ViewUrl
                : image.ViewUrl;
        }

        if (!string.IsNullOrWhiteSpace(image.Url))
        {
            return image.Url;
        }

        return null;
    }

    private void AddApiKeyHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-API-Key", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private static void EnsureSuccess(HttpResponseMessage response, string responseText)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string errorMessage = TryExtractApiError(responseText);
        throw new InvalidOperationException(
            $"Pixelfox request failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}). {errorMessage}");
    }

    private static string TryExtractApiError(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return "No error details returned.";
        }

        try
        {
            JObject? json = JsonConvert.DeserializeObject<JObject>(responseText);
            string? message = json?.Value<string>("message");
            if (!string.IsNullOrWhiteSpace(message))
            {
                return message;
            }
        }
        catch
        {
        }

        return responseText.Trim();
    }

    private static bool TryParseAlbumId(string? albumIdText, out long? albumId, out string? error)
    {
        albumId = null;
        error = null;

        if (string.IsNullOrWhiteSpace(albumIdText))
        {
            return true;
        }

        if (!long.TryParse(albumIdText.Trim(), out long parsedAlbumId) || parsedAlbumId <= 0)
        {
            error = "Pixelfox album ID must be a positive integer.";
            return false;
        }

        albumId = parsedAlbumId;
        return true;
    }

    private static MemoryStream PrepareSeekableStream(Stream stream)
    {
        if (stream is MemoryStream memoryStream && memoryStream.CanSeek)
        {
            memoryStream.Position = 0;
            return new MemoryStream(memoryStream.ToArray(), writable: false);
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        MemoryStream copy = new();
        stream.CopyTo(copy);
        copy.Position = 0;
        return copy;
    }

    private sealed class ProgressReadStream : Stream
    {
        private readonly Stream _inner;
        private readonly Action<long> _progressCallback;
        private long _bytesRead;

        public ProgressReadStream(Stream inner, Action<long> progressCallback)
        {
            _inner = inner;
            _progressCallback = progressCallback;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _inner.Read(buffer, offset, count);
            Report(read);
            return read;
        }

        public override int Read(Span<byte> buffer)
        {
            int read = _inner.Read(buffer);
            Report(read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int read = await _inner.ReadAsync(buffer, cancellationToken);
            Report(read);
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }

        private void Report(int bytesRead)
        {
            if (bytesRead <= 0)
            {
                return;
            }

            _bytesRead += bytesRead;
            _progressCallback(_bytesRead);
        }
    }

    private sealed class PixelfoxUploadSession
    {
        [JsonProperty("upload_url")]
        public string UploadUrl { get; set; } = string.Empty;

        [JsonProperty("token")]
        public string Token { get; set; } = string.Empty;

        [JsonProperty("max_bytes")]
        public long MaxBytes { get; set; }
    }

    private sealed class PixelfoxStorageUploadResponse
    {
        [JsonProperty("image_uuid")]
        public string ImageUuid { get; set; } = string.Empty;
    }

    private sealed class PixelfoxProcessingStatus
    {
        [JsonProperty("complete")]
        public bool Complete { get; set; }

        [JsonProperty("failed")]
        public bool Failed { get; set; }
    }

    private sealed class PixelfoxImageResource
    {
        [JsonProperty("url")]
        public string? Url { get; set; }

        [JsonProperty("view_url")]
        public string? ViewUrl { get; set; }
    }
}
