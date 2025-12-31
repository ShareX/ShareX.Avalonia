#region License Information (GPL v3)

/*
    ShareX.Avalonia - The Avalonia UI implementation of ShareX
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

using ShareX.Avalonia.Common;
using ShareX.Avalonia.Uploaders.FileUploaders;
using System.Collections.Specialized;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ShareX.Avalonia.Uploaders.Plugins.AmazonS3Plugin;

/// <summary>
/// Simplified Amazon S3 uploader - supports basic S3 uploads with AWS V4 signing
/// </summary>
public class AmazonS3Uploader : FileUploader
{
    private const string DefaultRegion = "us-east-1";
    private readonly S3ConfigModel _config;

    public AmazonS3Uploader(S3ConfigModel config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override UploadResult Upload(Stream stream, string fileName)
    {
        bool isPathStyleRequest = _config.UsePathStyleUrl || _config.BucketName.Contains(".");
        
        string scheme = "https://";
        string endpoint = _config.Region.Contains("amazonaws.com") 
            ? _config.Region 
            : $"s3.{_config.Region}.amazonaws.com";
        
        string host = isPathStyleRequest 
            ? endpoint 
            : $"{_config.BucketName}.{endpoint}";
        
        string algorithm = "AWS4-HMAC-SHA256";
        string credentialDate = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string region = GetRegion();
        string scope = string.Join("/", credentialDate, region, "s3", "aws4_request");
        string credential = string.Join("/", _config.AccessKeyId, scope);
        string timeStamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        string contentType = $"application/{Path.GetExtension(fileName).TrimStart('.')}";

        string hashedPayload = "UNSIGNED-PAYLOAD";
        
        string uploadPath = GetUploadPath(fileName);
        string resultURL = GenerateURL(uploadPath);
        
        OnEarlyURLCopyRequested(resultURL);

        var headers = new NameValueCollection
        {
            ["Host"] = host,
            ["Content-Length"] = stream.Length.ToString(),
            ["Content-Type"] = contentType,
            ["x-amz-date"] = timeStamp,
            ["x-amz-content-sha256"] = hashedPayload,
            ["x-amz-storage-class"] = _config.StorageClass.ToString()
        };

        if (_config.SetPublicACL)
        {
            headers["x-amz-acl"] = "public-read";
        }

        string canonicalURI = uploadPath;
        if (isPathStyleRequest) 
        {
            canonicalURI = "/" + _config.BucketName + "/" + canonicalURI.TrimStart('/');
        }
        else
        {
            canonicalURI = "/" + canonicalURI.TrimStart('/');
        }

        canonicalURI = Uri.EscapeDataString(canonicalURI).Replace("%2F", "/");
        
        string canonicalQueryString = "";
        string canonicalHeaders = CreateCanonicalHeaders(headers);
        string signedHeaders = GetSignedHeaders(headers);

        string canonicalRequest = "PUT\n" +
            canonicalURI + "\n" +
            canonicalQueryString + "\n" +
            canonicalHeaders + "\n" +
            signedHeaders + "\n" +
            hashedPayload;

        string stringToSign = algorithm + "\n" +
            timeStamp + "\n" +
            scope + "\n" +
            ComputeSHA256Hash(canonicalRequest);

        byte[] dateKey = ComputeHMACSHA256(_config.SecretAccessKey, "AWS4" + _config.SecretAccessKey).ToArray();
        byte[] dateRegionKey = ComputeHMACSHA256(region, dateKey).ToArray();
        byte[] dateRegionServiceKey = ComputeHMACSHA256("s3", dateRegionKey).ToArray();
        byte[] signingKey = ComputeHMACSHA256("aws4_request", dateRegionServiceKey).ToArray();

        string signature = BytesToHex(ComputeHMACSHA256(stringToSign, signingKey));

        headers["Authorization"] = algorithm + " " +
            "Credential=" + credential + "," +
            "SignedHeaders=" + signedHeaders + "," +
            "Signature=" + signature;

        headers.Remove("Host");
        headers.Remove("Content-Type");

        string url = scheme + host + canonicalURI;

        SendRequest(HttpMethod.PUT, url, stream, contentType, null, headers);

        if (LastResponseInfo != null && LastResponseInfo.IsSuccess)
        {
            return new UploadResult
            {
                IsSuccess = true,
                URL = resultURL
            };
        }

        Errors.Add("Upload to Amazon S3 failed.");
        return null;
    }

    private string GetRegion()
    {
        if (!string.IsNullOrEmpty(_config.Region))
        {
            // If it's already a region code, return it
            if (!_config.Region.Contains("."))
            {
                return _config.Region;
            }
        }
        return DefaultRegion;
    }

    private string GetUploadPath(string fileName)
    {
        string path = _config.ObjectPrefix.Trim('/');
        // Simple path parsing - replace %y with year, %mo with month
        path = path.Replace("%y", DateTime.Now.Year.ToString())
                   .Replace("%mo", DateTime.Now.Month.ToString("00"));
        
        return string.IsNullOrEmpty(path) ? fileName : $"{path}/{fileName}";
    }

    private string GenerateURL(string uploadPath)
    {
        if (_config.UseCustomCNAME && !string.IsNullOrEmpty(_config.CustomDomain))
        {
            return $"https://{_config.CustomDomain.TrimEnd('/')}/{uploadPath.TrimStart('/')}";
        }
        
        string endpoint = _config.Region.Contains("amazonaws.com") 
            ? _config.Region 
            : $"s3.{_config.Region}.amazonaws.com";
        
        return $"https://{endpoint}/{_config.BucketName}/{uploadPath.TrimStart('/')}";
    }

    private string CreateCanonicalHeaders(NameValueCollection headers)
    {
        var sorted = headers.AllKeys.OrderBy(k => k).Select(k => $"{k.ToLowerInvariant()}:{headers[k].Trim()}\n");
        return string.Join("", sorted);
    }

    private string GetSignedHeaders(NameValueCollection headers)
    {
        return string.Join(";", headers.AllKeys.OrderBy(k => k).Select(k => k.ToLowerInvariant()));
    }

    private string ComputeSHA256Hash(string text)
    {
        return BytesToHex(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private IEnumerable<byte> ComputeHMACSHA256(string data, string key)
    {
        return ComputeHMACSHA256(data, Encoding.UTF8.GetBytes(key));
    }

    private IEnumerable<byte> ComputeHMACSHA256(string data, byte[] key)
    {
        using (var hmac = new HMACSHA256(key))
        {
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }
    }

    private string BytesToHex(IEnumerable<byte> bytes)
    {
        return string.Concat(bytes.Select(b => b.ToString("x2")));
    }
}
