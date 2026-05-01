using System;

namespace Titanic.CDN.Tests
{
    /// <summary>
    /// Base class for all Titanic CDN tests.
    /// </summary>
    public abstract class TitanicCDNTest : IDisposable
    {
        public const string AccessKeyEnvName = "CDN_ACCESS_KEY";

        protected readonly TitanicCDN Cdn;
        protected bool HasAccessKey => Cdn.HasAccessKey;

        protected TitanicCDNTest()
        {
            Cdn = new TitanicCDN();

            string? accessKey = Environment.GetEnvironmentVariable(AccessKeyEnvName);
            if (!string.IsNullOrWhiteSpace(accessKey))
                Cdn.AccessKey = accessKey;
        }

        public void Dispose()
        {
            Cdn.Dispose();
        }
    }
}
