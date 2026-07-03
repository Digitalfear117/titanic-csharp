using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
#if NET5_0_OR_GREATER
using System.Net.Http;
#endif
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Titanic.API.Models;
using Titanic.API.Requests;
using Titanic.Helpers.Http;

namespace Titanic.API
{
    public class TitanicAPI : IDisposable
    {
        #if NET8_0_OR_GREATER
        private const DynamicallyAccessedMemberTypes types = DynamicallyAccessedMemberTypes.PublicConstructors |
                                                             DynamicallyAccessedMemberTypes.NonPublicConstructors |
                                                             DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
                                                             DynamicallyAccessedMemberTypes.PublicProperties |
                                                             DynamicallyAccessedMemberTypes.NonPublicProperties |
                                                             DynamicallyAccessedMemberTypes.PublicMethods |
                                                             DynamicallyAccessedMemberTypes.NonPublicMethods;
        #endif
        
#pragma warning disable CA1859
        private readonly IHttpInterface _http;
#pragma warning restore CA1859

        private string userAgent => $"Titanic.API/{packageVersion}";
        private string packageVersion => typeof(TitanicAPI).Assembly.GetName().Version?.ToString() ?? "Unknown";

        public TokenModel Token
        {
            get;
            set
            {
                field = value;

                this._http.RemoveDefaultHeader("Authorization");

                if (value != null)
                    this._http.AddDefaultHeader("Authorization", $"Bearer {value.AccessToken}");

                TokenUpdated?.Invoke(value);
            }
        }

        /// <summary>
        /// Raised whenever the token pair changes, i.e. after a login or refresh.
        /// </summary>
        public event Action<TokenModel> TokenUpdated;

        private readonly object _refreshLock = new();

        public bool IsLoggedIn => Token != null;
        public bool IsTokenExpired => Token == null || DateTime.Now > Token.ExpiresAt.AddSeconds(-30);

        public TitanicAPI(string baseUrl = "https://api.titanic.sh")
        {
            this._http = HttpInterfaceFactory.Create(baseUrl);
            this._http.AddDefaultHeader("User-Agent", userAgent);
        }

#if NET8_0_OR_GREATER
        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026",
            Justification = "The contract resolver is only used within already trim-compatible methods."
        )]
#endif
        private static readonly JsonSerializerSettings _settings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };


#if NET8_0_OR_GREATER
        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026",
            Justification = "The appropriate code is marked as untrimmed."
        )]
#endif
        private T Send
#if NET8_0_OR_GREATER
            <[DynamicallyAccessedMembers(types)] T>
#else
            <T>
#endif
            (HttpMethodType methodType, string endpoint, object content = null, Dictionary<string, string> headers = null, bool checkToken = true)
        {
            string str = PerformRequest(methodType, endpoint, content, headers, checkToken);

            T obj = JsonConvert.DeserializeObject<T>(str, _settings);
            if (obj == null)
                throw new Exception("Response had null content");

            return obj;
        }

#if NET8_0_OR_GREATER
        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026",
            Justification = "The appropriate list element type is marked as untrimmed."
        )]
#endif
        private List<T> SendList
#if NET8_0_OR_GREATER
            <[DynamicallyAccessedMembers(types)] T>
#else
            <T>
#endif
            (HttpMethodType methodType, string endpoint, object content = null, Dictionary<string, string> headers = null, bool checkToken = true)
        {
            string str = PerformRequest(methodType, endpoint, content, headers, checkToken);

            List<T> obj = JsonConvert.DeserializeObject<List<T>>(str, _settings);
            if (obj == null)
                throw new Exception("Response had null content");

            return obj;
        }

#if NET8_0_OR_GREATER
        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026",
            Justification = "The appropriate code is marked as untrimmed."
        )]
#endif
        private string PerformRequest(HttpMethodType methodType, string endpoint, object content, Dictionary<string, string> headers, bool checkToken)
        {
            if (checkToken)
                EnsureValidAccessToken();

            string jsonContent = null;
            string usedAccessToken = Token?.AccessToken;

            // Only serialize content for methods that support a body
            if (content != null && methodType != HttpMethodType.GET)
            {
                jsonContent = JsonConvert.SerializeObject(content, _settings);
                headers ??= new Dictionary<string, string>();
                headers["Content-Type"] = "application/json";
            }

            try
            {
                return this._http.RequestString(
                    methodType, endpoint,
                    jsonContent, headers
                );
            }
            catch (Exception e) when (checkToken && IsLoggedIn && IsUnauthorizedError(e))
            {
                // The access token was rejected before its expected expiry
                Debug.Print("TitanicAPI: Request was unauthorized, refreshing token & retrying...");
                RefreshAccessToken(usedAccessToken);

                return this._http.RequestString(
                    methodType, endpoint,
                    jsonContent, headers
                );
            }
        }

        private static bool IsUnauthorizedError(Exception e)
        {
            if (e is WebException webException && webException.Response is HttpWebResponse response)
                return response.StatusCode == HttpStatusCode.Unauthorized;

#if NET5_0_OR_GREATER
            if (e is HttpRequestException httpException)
                return httpException.StatusCode == HttpStatusCode.Unauthorized;
#endif

            return false;
        }

        public T Get
#if NET8_0_OR_GREATER
            <[DynamicallyAccessedMembers(types)] T>
#else
            <T>
#endif
            (string endpoint, Dictionary<string, string> headers = null)
        {
            Debug.Print("TitanicAPI: GET " + endpoint);
            return this.Send<T>(HttpMethodType.GET, endpoint, null, headers);
        }

        public List<T> GetList
#if NET8_0_OR_GREATER
            <[DynamicallyAccessedMembers(types)] T>
#else
            <T>
#endif
            (string endpoint, Dictionary<string, string> headers = null)
        {
            Debug.Print("TitanicAPI: GET " + endpoint);
            return this.SendList<T>(HttpMethodType.GET, endpoint, null, headers);
        }

        public T Post
#if NET8_0_OR_GREATER
            <[DynamicallyAccessedMembers(types)] T>
#else
            <T>
#endif
            (string endpoint, object data, Dictionary<string, string> headers = null)
        {
            Debug.Print("TitanicAPI: POST " + endpoint);
            return this.Send<T>(HttpMethodType.POST, endpoint, data, headers, RequiresTokenCheck(endpoint));
        }

        public List<T> PostList
#if NET8_0_OR_GREATER
            <[DynamicallyAccessedMembers(types)] T>
#else
            <T>
#endif
            (string endpoint, object data, Dictionary<string, string> headers = null)
        {
            Debug.Print("TitanicAPI: POST " + endpoint);
            return this.SendList<T>(HttpMethodType.POST, endpoint, data, headers, RequiresTokenCheck(endpoint));
        }

        public T Put
#if NET8_0_OR_GREATER
            <[DynamicallyAccessedMembers(types)] T>
#else
            <T>
#endif
            (string endpoint, object data, Dictionary<string, string> headers = null)
        {
            Debug.Print("TitanicAPI: PUT " + endpoint);
            return this.Send<T>(HttpMethodType.PUT, endpoint, data, headers);
        }

        public T Patch
#if NET8_0_OR_GREATER
            <[DynamicallyAccessedMembers(types)] T>
#else
            <T>
#endif
            (string endpoint, object data, Dictionary<string, string> headers = null)
        {
            Debug.Print("TitanicAPI: PATCH " + endpoint);
            return this.Send<T>(HttpMethodType.PATCH, endpoint, data, headers);
        }

        public T Delete
#if NET8_0_OR_GREATER
            <[DynamicallyAccessedMembers(types)] T>
#else
            <T>
#endif
             (string endpoint, Dictionary<string, string> headers = null)
        {
            Debug.Print("TitanicAPI: DELETE " + endpoint);
            return this.Send<T>(HttpMethodType.DELETE, endpoint, null, headers);
        }

        public List<T> DeleteList
#if NET8_0_OR_GREATER
            <[DynamicallyAccessedMembers(types)] T>
#else
            <T>
#endif
             (string endpoint, Dictionary<string, string> headers = null)
        {
            Debug.Print("TitanicAPI: DELETE " + endpoint);
            return this.SendList<T>(HttpMethodType.DELETE, endpoint, null, headers);
        }

        public byte[] Download(string url)
        {
            return this._http.RequestBytes(HttpMethodType.GET, url, (string)null, null);
        }

        private static bool RequiresTokenCheck(string endpoint)
        {
            // Skip token checking for auth endpoints, since refresh would
            // recurse infinitely, and login would supply its own credentials
            return endpoint != "/account/refresh" && endpoint != "/account/login";
        }

        public void EnsureValidAccessToken()
        {
            if (!IsLoggedIn || !IsTokenExpired)
                return;

            lock (_refreshLock)
            {
                // Another thread may have refreshed while we were waiting on the lock
                if (!IsLoggedIn || !IsTokenExpired)
                    return;

                RefreshTokenRequest request = new(Token.RefreshToken);
                request.BlockingPerform(this);
                Debug.Print("TitanicAPI: Access token refreshed (EnsureValidAccessToken)");
            }
        }

        private void RefreshAccessToken(string staleAccessToken)
        {
            lock (_refreshLock)
            {
                if (!IsLoggedIn)
                    return;

                // Another thread may have already replaced the rejected token
                if (staleAccessToken != null && Token.AccessToken != staleAccessToken)
                    return;

                RefreshTokenRequest request = new(Token.RefreshToken);
                request.BlockingPerform(this);
                Debug.Print("TitanicAPI: Access token refreshed (RefreshAccessToken)");
            }
        }

        public void Dispose()
        {
            this._http.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
