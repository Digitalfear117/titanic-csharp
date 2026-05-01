using System.Linq;
using Titanic.API.Http;
using Titanic.API.Models;

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
    public readonly List<UpdateInformation> UpdatePath = new();
    public bool PatchUpdatePathComplete = true;

    public UpdateInformation(ModdedReleaseEntryModel moddedEntry, string identifier) : this(moddedEntry.DownloadUrl, identifier, moddedEntry.Version)
    {}

    public UpdateInformation(ModdedReleaseUpdateModel update, string identifier) : this(update.TargetRelease, identifier)
    {
        if (update.Path == null)
            return;

        this.PatchUpdatePathComplete = true;
        for (int i = 0; i < update.Path.Count; i++)
        {
            ModdedReleaseEntryModel entry = update.Path[i];

            if (!string.IsNullOrEmpty(entry.UpdateUrl))
            {
                this.UpdatePath.Add(new UpdateInformation(entry.UpdateUrl, identifier, entry.Version));
            }
            else
            {
                // Not every entry in the path has a patch url, so we
                // have to fall back to the full archive for this update
                this.PatchUpdatePathComplete = false;
            }
        }
    }

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
    /// Do we have a complete patch path to the target release?
    /// </summary>
    public bool HasPatchUpdatePath => this.PatchUpdatePathComplete && this.UpdatePath.Count > 0;

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
