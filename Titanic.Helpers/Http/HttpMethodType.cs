using System.Diagnostics.CodeAnalysis;

namespace Titanic.Helpers.Http;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum HttpMethodType
{
    GET,
    POST,
    PUT,
    PATCH,
    DELETE
}
