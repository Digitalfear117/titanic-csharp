using Titanic.API.Requests;
using Xunit;

namespace Titanic.API.Tests
{
    /// <summary>
    /// Tests for release-related API requests.
    /// </summary>
    public class ReleaseRequestTests : TitanicAPITest
    {
        [Fact]
        public void GetModdedReleaseUpdate_ByVersionAndStream_ReturnsUpdatePath()
        {
            // Arrange
            var request = new GetModdedReleaseUpdateRequest("digital", version: "5.1.0", stream: "net20-x86");

            // Act
            var update = request.BlockingPerform(Api);

            // Assert
            Assert.NotNull(update);
            Assert.NotNull(update.Client);
            Assert.Equal("digital", update.Client.ClientExtension);
            Assert.Equal("net20-x86", update.Stream);
            Assert.NotNull(update.TargetRelease);
            Assert.False(string.IsNullOrEmpty(update.TargetRelease.Version));
            Assert.NotNull(update.Path);
        }
    }
}
