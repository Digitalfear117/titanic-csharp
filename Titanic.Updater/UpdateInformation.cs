using System.Linq;
using Titanic.API.Models;
using Titanic.Helpers.Http;

namespace Titanic.Updater;

public class UpdateInformation
{
    private Uri _downloadUri = null!;

    public string DownloadUrl
    {
        get;
        private set
        {
            field = value;
            this._downloadUri = new Uri(value);
        }
    }

    public readonly string ClientIdentifier;
    public readonly string Version;

    public UpdateInformation(ModdedReleaseEntryModel moddedEntry, string identifier) : this(moddedEntry.DownloadUrl, identifier, moddedEntry.Version)
    {}

    public UpdateInformation(TitanicReleaseModel titanicRelease) : this(titanicRelease.Downloads.First(), titanicRelease.Name, titanicRelease.Name)
    {}

    public UpdateInformation(string downloadUrl, string clientIdentifier, string version)
    {
        this.DownloadUrl = downloadUrl;
        this.ClientIdentifier = clientIdentifier;
        this.Version = version;
        
        // Try to resolve external download URLs, e.g. mediafire links -> direct download links
        this.ResolveExternalDownloadUrls();
    }

    /// <summary>
    /// Can we directly download this file?
    /// </summary>
    public bool IsDownloadable => Path.GetFileName(this._downloadUri.AbsolutePath).Contains(".");

    /// <summary>
    /// Are we able to extract this file?
    /// </summary>
    public bool IsExtractable => this._downloadUri.AbsolutePath.EndsWith(".zip");

    private bool ResolveExternalDownloadUrls()
    {
        if (!this._downloadUri.Host.Contains("mediafire.com"))
            return false;

        IHttpInterface http = HttpInterfaceFactory.Create("https://mediafire.com");
        MediaFireDownloader downloader = new(http);

        MediaFireDownloader.DownloadItem? downloadItem = downloader.FetchDirectDownloadUrl(this.DownloadUrl);
        if (downloadItem == null)
            return false;

        this.DownloadUrl = downloadItem.DownloadUrl;
        return true;
    }
}
