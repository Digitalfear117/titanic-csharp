using System;
using System.Collections.Generic;
using Titanic.CDN.Models;

namespace Titanic.CDN.Requests
{
    public class UploadFileRequest : CDNRequest<AdminUploadResponseModel>
    {
        public string ObjectKey { get; set; }
        public byte[] Data { get; set; }
        public string? ContentType { get; set; }
        public string? CacheControl { get; set; }
        public string? ContentDisposition { get; set; }

        public UploadFileRequest(
            string objectKey,
            byte[] data,
            string? contentType = null,
            string? cacheControl = null,
            string? contentDisposition = null)
        {
            TitanicCDN.EscapeObjectKey(objectKey);
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ObjectKey = objectKey;
            Data = data;
            ContentType = contentType;
            CacheControl = cacheControl;
            ContentDisposition = contentDisposition;
        }

        protected override AdminUploadResponseModel Execute(TitanicCDN cdn)
        {
            cdn.EnsureAccessKey();

            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(ContentType))
                headers["Content-Type"] = ContentType!;
            if (!string.IsNullOrEmpty(CacheControl))
                headers["Cache-Control"] = CacheControl!;
            if (!string.IsNullOrEmpty(ContentDisposition))
                headers["Content-Disposition"] = ContentDisposition!;

            return cdn.Put<AdminUploadResponseModel>($"/admin/files/{TitanicCDN.EscapeObjectKey(ObjectKey)}", Data, headers);
        }
    }
}
