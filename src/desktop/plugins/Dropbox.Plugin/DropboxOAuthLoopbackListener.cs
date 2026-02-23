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

using System.Net;
using System.Text;

namespace ShareX.Dropbox.Plugin;

internal sealed class DropboxOAuthCallbackResult
{
    public bool IsSuccess { get; init; }
    public string? Code { get; init; }
    public string? Error { get; init; }
    public string? ErrorDescription { get; init; }
}

internal sealed class DropboxOAuthLoopbackListener : IDisposable
{
    private readonly Uri _redirectUri;
    private readonly HttpListener _listener;

    public DropboxOAuthLoopbackListener(Uri redirectUri)
    {
        _redirectUri = redirectUri ?? throw new ArgumentNullException(nameof(redirectUri));
        _listener = new HttpListener();
        _listener.Prefixes.Add(BuildPrefix(redirectUri));
    }

    public void Start()
    {
        _listener.Start();
    }

    public async Task<DropboxOAuthCallbackResult> WaitForCallbackAsync(string expectedState, CancellationToken cancellation)
    {
        using var registration = cancellation.Register(static state =>
        {
            var listener = (HttpListener)state!;
            try
            {
                listener.Stop();
            }
            catch
            {
                // ignored
            }
        }, _listener);

        while (!cancellation.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException) when (cancellation.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellation);
            }
            catch (ObjectDisposedException) when (cancellation.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellation);
            }

            if (!IsExpectedPath(context.Request.Url))
            {
                await WriteResponseAsync(context.Response, false, "Dropbox login is in progress. You can close this tab.", cancellation)
                    .ConfigureAwait(false);
                continue;
            }

            string? error = context.Request.QueryString["error"];
            string? errorDescription = context.Request.QueryString["error_description"];
            string? code = context.Request.QueryString["code"];
            string? state = context.Request.QueryString["state"];

            if (!string.Equals(state, expectedState, StringComparison.Ordinal))
            {
                await WriteResponseAsync(context.Response, true, "Login state validation failed. Return to the app and try again.", cancellation)
                    .ConfigureAwait(false);
                return new DropboxOAuthCallbackResult
                {
                    IsSuccess = false,
                    Error = "state_mismatch",
                    ErrorDescription = "State mismatch in OAuth callback."
                };
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                string message = string.IsNullOrWhiteSpace(errorDescription) ? error : errorDescription;
                await WriteResponseAsync(context.Response, true, $"Dropbox authorization failed: {message}", cancellation)
                    .ConfigureAwait(false);
                return new DropboxOAuthCallbackResult
                {
                    IsSuccess = false,
                    Error = error,
                    ErrorDescription = errorDescription
                };
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                await WriteResponseAsync(context.Response, true, "Dropbox authorization code is missing.", cancellation)
                    .ConfigureAwait(false);
                return new DropboxOAuthCallbackResult
                {
                    IsSuccess = false,
                    Error = "missing_code",
                    ErrorDescription = "Authorization code is missing."
                };
            }

            await WriteResponseAsync(context.Response, false, "Dropbox login completed. You can close this tab and return to XerahS.", cancellation)
                .ConfigureAwait(false);
            return new DropboxOAuthCallbackResult
            {
                IsSuccess = true,
                Code = code
            };
        }

        throw new OperationCanceledException(cancellation);
    }

    public void Dispose()
    {
        if (_listener.IsListening)
        {
            try
            {
                _listener.Stop();
            }
            catch
            {
                // ignored
            }
        }

        _listener.Close();
    }

    private bool IsExpectedPath(Uri? requestUri)
    {
        if (requestUri == null)
        {
            return false;
        }

        string expectedPath = NormalizePath(_redirectUri.AbsolutePath);
        string actualPath = NormalizePath(requestUri.AbsolutePath);
        return string.Equals(expectedPath, actualPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildPrefix(Uri redirectUri)
    {
        string host = redirectUri.Host;
        int port = redirectUri.Port;
        return $"{redirectUri.Scheme}://{host}:{port}/";
    }

    private static string NormalizePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "/";
        }

        string normalized = "/" + value.Trim('/');
        return normalized == "/" ? "/" : normalized;
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, bool isError, string message, CancellationToken cancellation)
    {
        string textColor = isError ? "#9f1239" : "#166534";
        string title = isError ? "Dropbox Login Failed" : "Dropbox Login Complete";
        string safeMessage = WebUtility.HtmlEncode(message);

        string html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8""/>
  <title>{title}</title>
  <meta name=""viewport"" content=""width=device-width, initial-scale=1""/>
  <style>
    body {{ font-family: Segoe UI, sans-serif; background: #f8fafc; color: #0f172a; margin: 0; }}
    main {{ max-width: 560px; margin: 10vh auto; background: #ffffff; padding: 24px; border-radius: 12px; border: 1px solid #e2e8f0; }}
    h1 {{ margin: 0 0 12px; font-size: 22px; color: {textColor}; }}
    p {{ margin: 0; line-height: 1.5; }}
  </style>
</head>
<body>
  <main>
    <h1>{title}</h1>
    <p>{safeMessage}</p>
  </main>
</body>
</html>";

        byte[] bytes = Encoding.UTF8.GetBytes(html);
        response.StatusCode = 200;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.LongLength;
        await response.OutputStream.WriteAsync(bytes, cancellation).ConfigureAwait(false);
        response.OutputStream.Close();
    }
}
