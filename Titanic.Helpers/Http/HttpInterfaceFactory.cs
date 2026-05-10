using System;

namespace Titanic.Helpers.Http;

public static class HttpInterfaceFactory
{
    public static IHttpInterface Create(string baseAddress)
    {
#if NET5_0_OR_GREATER
        return new HttpClientInterface(baseAddress);
#else
        return new WebClientInterface(baseAddress);
#endif
    }
}
