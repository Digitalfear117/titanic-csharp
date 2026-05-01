namespace Titanic.Updater;

#nullable disable

public enum DownloadedUpdateKind
{
    FullArchive,
    PatchUpdatePath
}

public class DownloadedUpdate
{
    public DownloadedUpdateKind Kind;
    public string ClientIdentifier;
    public string Filename;
    public string Path;
    public string FullArchiveUrl;
    public string FullArchivePath;
    public List<DownloadedUpdatePart> Parts = new();
}

public class DownloadedUpdatePart
{
    public string ClientIdentifier;
    public string Filename;
    public string Path;
    public string Version;
}
