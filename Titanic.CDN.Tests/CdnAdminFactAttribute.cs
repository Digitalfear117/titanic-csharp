using System;
using Xunit;

namespace Titanic.CDN.Tests
{
    public sealed class CdnAdminFactAttribute : FactAttribute
    {
        public CdnAdminFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(TitanicCDNTest.AccessKeyEnvName)))
                Skip = $"Set {TitanicCDNTest.AccessKeyEnvName} to run admin integration tests.";
        }
    }
}
