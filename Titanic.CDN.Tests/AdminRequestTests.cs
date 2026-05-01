using Titanic.CDN.Requests;

namespace Titanic.CDN.Tests
{
    /// <summary>
    /// Tests for authenticated CDN admin requests.
    /// </summary>
    public class AdminRequestTests : TitanicCDNTest
    {
        [CdnAdminFact]
        public void GetSession_WithAccessKey_ReturnsSession()
        {
            // Arrange
            var request = new GetSessionRequest();

            // Act
            var session = request.BlockingPerform(Cdn);

            // Assert
            Assert.NotNull(session);
            Assert.False(string.IsNullOrEmpty(session.Name));
            Assert.NotNull(session.Prefixes);
            Assert.NotNull(session.Permissions);
        }

        [CdnAdminFact]
        public void ListFiles_WithAccessKey_ReturnsListResponse()
        {
            // Arrange
            var sessionRequest = new GetSessionRequest();
            var session = sessionRequest.BlockingPerform(Cdn);
            Assert.NotNull(session.Prefixes);
            Assert.NotEmpty(session.Prefixes);

            string prefix = session.Prefixes[0];
            var request = new ListFilesRequest(prefix, limit: 100);

            // Act
            var files = request.BlockingPerform(Cdn);

            // Assert
            Assert.NotNull(files);
            Assert.Equal(prefix, files.Prefix);
            Assert.NotNull(files.Items);
            Assert.NotNull(files.NextCursor);
        }
    }
}
