using Titanic.CDN.Models;

namespace Titanic.CDN.Requests
{
    public class GetSessionRequest : CDNRequest<AdminSessionModel>
    {
        protected override AdminSessionModel Execute(TitanicCDN cdn)
        {
            cdn.EnsureAccessKey();
            return cdn.Get<AdminSessionModel>("/admin/session");
        }
    }
}
