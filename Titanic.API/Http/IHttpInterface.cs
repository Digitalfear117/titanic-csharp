using System;
using System.Collections.Generic;

namespace Titanic.API.Http;

#nullable enable

public interface IHttpInterface : IDisposable
{
    string RequestString(HttpMethodType methodType, string endpoint, string? content, Dictionary<string, string>? headers);
    byte[] RequestBytes(HttpMethodType methodType, string endpoint, string? content, Dictionary<string, string>? headers);
    byte[] RequestBytes(HttpMethodType methodType, string endpoint, byte[]? content, Dictionary<string, string>? headers);
    void RequestEmpty(HttpMethodType methodType, string endpoint, string? content, Dictionary<string, string>? headers);
    void RequestEmpty(HttpMethodType methodType, string endpoint, byte[]? content, Dictionary<string, string>? headers);
    void AddDefaultHeader(string key, string value);
    void RemoveDefaultHeader(string key);
}
