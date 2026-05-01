#if SUPPORT_HTTPCLIENT
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Titanic.Helpers.Http;

#nullable enable

public class HttpClientInterface : IHttpInterface
{
    private readonly HttpClient _client;

    public HttpClientInterface(string baseAddress)
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri(baseAddress)
        };
    }

    private HttpResponseMessage Send(HttpMethodType methodType, string endpoint, HttpContent? content, Dictionary<string, string>? headers)
    {
        HttpMethod method = methodType switch
        {
            HttpMethodType.GET => HttpMethod.Get,
            HttpMethodType.POST => HttpMethod.Post,
            HttpMethodType.PUT => HttpMethod.Put,
            #if NET5_0_OR_GREATER
            HttpMethodType.PATCH => HttpMethod.Patch,
            #else
            HttpMethodType.PATCH => new HttpMethod("PATCH"),
            #endif
            HttpMethodType.DELETE => HttpMethod.Delete,
            _ => throw new ArgumentOutOfRangeException(nameof(methodType), methodType, null)
        };

        HttpRequestMessage request = new(method, endpoint);
        request.Content = content;

        if (headers != null)
        {
            foreach (KeyValuePair<string, string> kvp in headers)
            {
                if (request.Content != null && IsContentHeader(kvp.Key))
                    request.Content.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                else
                    request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }
        }

        HttpResponseMessage response = this._client.SendAsync(request).Result;
        response.EnsureSuccessStatusCode();

        return response;
    }

    private static bool IsContentHeader(string key)
    {
        return key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Content-Language", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Content-Location", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Content-MD5", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Content-Range", StringComparison.OrdinalIgnoreCase);
    }

    private static StringContent? CreateStringContent(string? content)
    {
        return content == null ? null : new StringContent(content, Encoding.UTF8, "application/json");
    }

    private static ByteArrayContent? CreateByteContent(byte[]? content)
    {
        return content == null ? null : new ByteArrayContent(content);
    }

    public string RequestString(HttpMethodType methodType, string endpoint, string? content, Dictionary<string, string>? headers)
    {
        HttpResponseMessage response = Send(methodType, endpoint, CreateStringContent(content), headers);
        return response.Content.ReadAsStringAsync().Result;
    }

    public byte[] RequestBytes(HttpMethodType methodType, string endpoint, string? content, Dictionary<string, string>? headers)
    {
        HttpResponseMessage response = Send(methodType, endpoint, CreateStringContent(content), headers);
        return response.Content.ReadAsByteArrayAsync().Result;
    }

    public byte[] RequestBytes(HttpMethodType methodType, string endpoint, byte[]? content, Dictionary<string, string>? headers)
    {
        HttpResponseMessage response = Send(methodType, endpoint, CreateByteContent(content), headers);
        return response.Content.ReadAsByteArrayAsync().Result;
    }

    public void RequestEmpty(HttpMethodType methodType, string endpoint, string? content, Dictionary<string, string>? headers)
    {
        Send(methodType, endpoint, CreateStringContent(content), headers).Dispose();
    }

    public void RequestEmpty(HttpMethodType methodType, string endpoint, byte[]? content, Dictionary<string, string>? headers)
    {
        Send(methodType, endpoint, CreateByteContent(content), headers).Dispose();
    }

    public void AddDefaultHeader(string key, string value)
    {
        this._client.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
    }

    public void RemoveDefaultHeader(string key)
    {
        this._client.DefaultRequestHeaders.Remove(key);
    }

    public void Dispose()
    {
        this._client.Dispose();
        GC.SuppressFinalize(this);
    }
}
#endif
