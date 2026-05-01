using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using Titanic.API;
using Titanic.API.Models;
using Titanic.API.Requests;

namespace Titanic.Updater;

public class UpdateManager : IDisposable
{
    private readonly TitanicAPI _api = new();
    private readonly UpdateManagerSettings _settings;

    public UpdateManager(UpdateManagerSettings settings)
    {
        this._settings = settings;

        if (settings.SharpZipLibCodePage != null)
        {
#if !NET45_OR_GREATER && !NET8_0_OR_GREATER
            ZipConstants.DefaultCodePage = settings.SharpZipLibCodePage.Value;
#elif !NET8_0_OR_GREATER
            ZipStrings.CodePage = settings.SharpZipLibCodePage.Value;
#endif
        }

        // Clean up old the executable we made if we just updated
        if (this._settings.ReplaceCurrentExecutable)
        {
            string processPath = ExecutablePath + ".old";
            if (File.Exists(processPath))
                File.Delete(processPath);
        }
    }

    public UpdateInformation? CheckUpdateForClient(ModdedClientInformation clientInfo)
    {
        GetModdedReleaseUpdateRequest request = new(
            clientInfo.ClientIdentifier,
            version: clientInfo.InstalledVersion,
            stream: clientInfo.InstalledStream
        );
        ModdedReleaseUpdateModel update = request.BlockingPerform(this._api);

        return update.TargetRelease == null
            ? null
            : new UpdateInformation(update.TargetRelease, clientInfo.ClientIdentifier);
    }

    public DownloadedUpdate DownloadClientUpdate(UpdateInformation update)
    {
        if (!update.IsExtractable)
            throw new Exception("Update is not extractable (not a .zip), check update.IsExtractable and prompt the user to open the update.DownloadUrl instead or repackage your updates");

        string updatePath = Path.Combine(_settings.DataDirectory, $"{update.ClientIdentifier}{Path.DirectorySeparatorChar}");
        if (!Directory.Exists(updatePath))
            Directory.CreateDirectory(updatePath);

        string filename = Path.GetFileName(update.DownloadUrl);
        string path = Path.Combine(updatePath, filename);

        DownloadedUpdate downloadedUpdate = new()
        {
            Kind = DownloadedUpdateKind.FullArchive,
            Filename = filename,
            Path = path,
            ClientIdentifier = update.ClientIdentifier,
            FullArchiveUrl = update.DownloadUrl,
            FullArchivePath = path,
        };

        if (File.Exists(path))
            return downloadedUpdate;

        byte[] data = this._api.Download(update.DownloadUrl);
        File.WriteAllBytes(path, data);

        return downloadedUpdate;
    }

    public void InstallClientUpdate(DownloadedUpdate update)
    {
        string staging = Path.Combine(_settings.DataDirectory, "_staging");
        if (!Directory.Exists(staging))
            Directory.CreateDirectory(staging);

        ZipUtil.Extract(update.Path, staging);

        if (this._settings.ReplaceCurrentExecutable)
        {
            string processPath = ExecutablePath;
            File.Move(processPath, processPath + ".old");
        }

        string outputDir = _settings.OutputPath;
        if (string.IsNullOrEmpty(outputDir))
            outputDir = Environment.CurrentDirectory;

        if (this._settings.IncludeClientIdentifierInOutputPath)
            outputDir = Path.Combine(outputDir, update.ClientIdentifier + '/');

        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        string[] files = Directory.GetFiles(staging, "*.*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string dest = Path.Combine(outputDir, file.Replace(staging + '/', ""));
            if (File.Exists(dest))
                File.Delete(dest);

            File.Move(file, dest);
        }

#if NET10_0_OR_GREATER
        string osuExecutable = Path.Combine(outputDir, "osu!");
        if (OperatingSystem.IsLinux() && File.Exists(osuExecutable))
        {
            UnixFileMode fileMode = File.GetUnixFileMode(osuExecutable);
            fileMode |= UnixFileMode.GroupExecute | UnixFileMode.OtherExecute | UnixFileMode.UserExecute;
            File.SetUnixFileMode(osuExecutable, fileMode);
        }
#endif

        this._settings.Exit?.Invoke();
    }

    private static string ExecutablePath =>
#if NET10_0_OR_GREATER
        Environment.ProcessPath!;
#else
        Assembly.GetEntryAssembly()!.Location;
#endif

    public void Dispose()
    {
        _api.Dispose();
        GC.SuppressFinalize(this);
    }
}
