namespace Titanic.CDN.Requests
{
    public class DeleteFileRequest : CDNRequest<object>
    {
        public string ObjectKey { get; set; }

        public DeleteFileRequest(string objectKey)
        {
            TitanicCDN.EscapeObjectKey(objectKey);
            ObjectKey = objectKey;
        }

        protected override object Execute(TitanicCDN cdn)
        {
            cdn.EnsureAccessKey();
            cdn.Delete($"/admin/files/{TitanicCDN.EscapeObjectKey(ObjectKey)}");
            return null!;
        }
    }
}
