using Titanic.CDN.Requests;

namespace Titanic.CDN.Tests
{
    /// <summary>
    /// Tests for public CDN requests.
    /// </summary>
    public class PublicRequestTests : TitanicCDNTest
    {
        [Fact]
        public void GetHealth_ReturnsNonEmptyResponse()
        {
            // Arrange
            var request = new GetHealthRequest();

            // Act
            string health = request.BlockingPerform(Cdn);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(health));
        }
    }
}
