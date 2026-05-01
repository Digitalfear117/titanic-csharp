namespace Titanic.CDN.Requests
{
    public class GetHealthRequest : CDNRequest<string>
    {
        protected override string Execute(TitanicCDN cdn)
        {
            return cdn.GetString("/health");
        }
    }
}
