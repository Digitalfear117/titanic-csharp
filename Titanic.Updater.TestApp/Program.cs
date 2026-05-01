using Titanic.API;
using Titanic.API.Models;
using Titanic.API.Requests;
using Titanic.Updater;
using Titanic.Updater.Versioning;

Console.WriteLine("Testing modded download");

ModdedClientInformation client = new()
{
    ClientIdentifier = "retrogecko",
    VersionKind = OsuVersionKind.BuildNumber,
    InstalledStream = "net10.0-linux-x64",
    InstalledVersion = "b1076",
};

UpdateManagerSettings settings = new()
{
    Exit = () => Console.WriteLine("EXIT CALLED!"),
    ReplaceCurrentExecutable = false,
    IncludeClientIdentifierInOutputPath = true,
};

using UpdateManager manager = new(settings);
UpdateInformation? update = manager.CheckUpdateForClient(client);
DownloadedUpdate downloadedUpdate;

if (update != null)
{
    if (!update.HasPatchUpdatePath && !update.IsExtractable)
    {
        Console.WriteLine($"Update found: {update.Version}, but not extractable");
        return;
    }
    Console.WriteLine($"Update found: {update.Version}" + (update.HasPatchUpdatePath ? " (patch)" : ""));
    downloadedUpdate = manager.DownloadClientUpdate(update);
    Console.WriteLine($"Update downloaded to {downloadedUpdate.Path}");
    manager.InstallClientUpdate(downloadedUpdate);
}
else
{
    Console.WriteLine("No updates were found.");
}

Console.WriteLine("Testing vanilla download");

using TitanicAPI api = new();
List<TitanicReleaseModel> req = new GetTitanicReleasesRequest().BlockingPerform(api);
TitanicReleaseModel release = req.First();

update = new UpdateInformation(release);
if (!update.IsExtractable)
{
    Console.WriteLine($"Update found: {update.Version}, but not extractable");
    return;
}
Console.WriteLine($"Update found: {update.Version}");
downloadedUpdate = manager.DownloadClientUpdate(update);
Console.WriteLine($"Update downloaded to {downloadedUpdate.Path}");
manager.InstallClientUpdate(downloadedUpdate);
