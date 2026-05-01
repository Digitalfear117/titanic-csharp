namespace Titanic.CDN.Requests
{
    public class DownloadObjectRequest : CDNRequest<byte[]>
    {
        public string ObjectKey { get; set; }
        public string? Range { get; set; }

        public DownloadObjectRequest(string objectKey, string? range = null)
        {
            ObjectKey = objectKey;
            Range = range;
        }

        protected override byte[] Execute(TitanicCDN cdn)
        {
            return cdn.Download(ObjectKey, Range);
        }
    }
}
