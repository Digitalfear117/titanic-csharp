using System.Reflection;
using System.Text;
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
            : new UpdateInformation(update, clientInfo.ClientIdentifier);
    }

    public DownloadedUpdate DownloadClientUpdate(UpdateInformation update)
    {
        bool canUsePatchUpdate = update.HasPatchUpdatePath && CanDownloadPatchUpdatePath(update);

        if (this._settings.PreferPatchUpdates && canUsePatchUpdate)
            return DownloadPatchUpdatePath(update);

        return DownloadFullUpdate(update);
    }

    private DownloadedUpdate DownloadFullUpdate(UpdateInformation update)
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

    private DownloadedUpdate DownloadPatchUpdatePath(UpdateInformation update)
    {
        string updatePath = Path.Combine(_settings.DataDirectory, update.ClientIdentifier + Path.DirectorySeparatorChar + "_manifests");

        if (!Directory.Exists(updatePath))
            Directory.CreateDirectory(updatePath);

        DownloadedUpdate downloadedUpdate = new()
        {
            Kind = DownloadedUpdateKind.PatchUpdatePath,
            ClientIdentifier = update.ClientIdentifier,
            FullArchiveUrl = update.DownloadUrl,
        };

        for (int i = 0; i < update.UpdatePath.Count; i++)
        {
            UpdateInformation pathUpdate = update.UpdatePath[i];

            string filename = GetManifestFilename(pathUpdate, i);
            if (string.IsNullOrEmpty(filename))
                filename = $"{i:D3}_{SanitizeFilename(pathUpdate.Version)}_update.json";

            string path = Path.Combine(updatePath, filename);
            UpdateManifest manifest;

            if (!File.Exists(path))
            {
                byte[] data = this._api.Download(pathUpdate.DownloadUrl);
                File.WriteAllBytes(path, data);
                manifest = UpdateManifestReader.ReadFromJson(Encoding.UTF8.GetString(data));
            }
            else
            {
                manifest = UpdateManifestReader.ReadFromFile(path);
            }

            UpdateManifestValidator.Validate(manifest, pathUpdate.ClientIdentifier);

            downloadedUpdate.Parts.Add(new DownloadedUpdatePart
            {
                ClientIdentifier = pathUpdate.ClientIdentifier,
                Filename = filename,
                Path = path,
                Version = pathUpdate.Version,
                ManifestUrl = pathUpdate.DownloadUrl,
                Manifest = manifest,
            });
        }

        if (downloadedUpdate.Parts.Count > 0)
        {
            downloadedUpdate.Filename = downloadedUpdate.Parts[downloadedUpdate.Parts.Count - 1].Filename;
            downloadedUpdate.Path = downloadedUpdate.Parts[downloadedUpdate.Parts.Count - 1].Path;
        }
        return downloadedUpdate;
    }

    private static bool CanDownloadPatchUpdatePath(UpdateInformation update)
    {
        return update.UpdatePath.Count > 0;
    }

    private static string GetManifestFilename(UpdateInformation update, int index)
    {
        string path = update.DownloadUrl;
        if (Uri.TryCreate(update.DownloadUrl, UriKind.RelativeOrAbsolute, out Uri? uri))
            path = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;

        string name = Path.GetFileName(path);

        if (string.IsNullOrEmpty(name))
            name = "update.json";

        return $"{SanitizeFilename(update.Version)}_{SanitizeFilename(name)}";
    }

    private static string SanitizeFilename(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "unknown";

        char[] invalidChars = Path.GetInvalidFileNameChars();
        char[] chars = value.ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(invalidChars, chars[i]) >= 0 || chars[i] == '/' || chars[i] == '\\')
                chars[i] = '_';
        }

        return new string(chars);
    }

    public void InstallClientUpdate(DownloadedUpdate update)
    {
        if (update.Kind == DownloadedUpdateKind.PatchUpdatePath)
        {
            InstallPatchUpdate(update);
            return;
        }

        InstallFullArchiveUpdate(update);
    }

    private void InstallFullArchiveUpdate(DownloadedUpdate update)
    {
        string staging = Path.Combine(_settings.DataDirectory, "_staging");
        if (Directory.Exists(staging))
            Directory.Delete(staging, true);

        if (!Directory.Exists(staging))
            Directory.CreateDirectory(staging);

        ZipUtil.Extract(update.Path, staging);

        if (this._settings.ReplaceCurrentExecutable)
        {
            string processPath = ExecutablePath;
            File.Move(processPath, processPath + ".old");
        }

        string outputDir = GetOutputDirectory(update.ClientIdentifier);

        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        string[] files = Directory.GetFiles(staging, "*.*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string relativePath = UpdatePathUtil.GetRelativePath(staging, file);
            string dest = UpdatePathUtil.CombineSafe(outputDir, relativePath);
            if (File.Exists(dest))
                File.Delete(dest);

            string? directory = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.Move(file, dest);
        }

        SetLinuxExecutableBit(outputDir);

        this._settings.Exit?.Invoke();
    }

    private void InstallPatchUpdate(DownloadedUpdate update)
    {
        string staging = Path.Combine(_settings.DataDirectory, "_staging");
        if (Directory.Exists(staging))
            Directory.Delete(staging, true);

        Directory.CreateDirectory(staging);

        string outputDir = GetOutputDirectory(update.ClientIdentifier);
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        try
        {
            PatchUpdateApplier applier = new(this._settings, outputDir, staging, ExecutablePath, this._api.Download);
            applier.Apply(update.Parts);
            SetLinuxExecutableBit(outputDir);
            this._settings.Exit?.Invoke();
        }
        catch (Exception ex)
        {
            if (!this._settings.FallbackToFullArchive)
                throw new PatchUpdateException("Patch update failed and full archive fallback is disabled", ex);

            DownloadedUpdate fullUpdate = DownloadFallbackFullUpdate(update);
            InstallFullArchiveUpdate(fullUpdate);
        }
    }

    private DownloadedUpdate DownloadFallbackFullUpdate(DownloadedUpdate update)
    {
        if (!string.IsNullOrEmpty(update.FullArchivePath) && File.Exists(update.FullArchivePath))
        {
            return new DownloadedUpdate
            {
                Kind = DownloadedUpdateKind.FullArchive,
                ClientIdentifier = update.ClientIdentifier,
                Filename = Path.GetFileName(update.FullArchivePath),
                Path = update.FullArchivePath,
                FullArchivePath = update.FullArchivePath,
            };
        }

        if (string.IsNullOrEmpty(update.FullArchiveUrl))
            throw new PatchUpdateException("Patch update failed and no full archive fallback URL is available");

        UpdateInformation fallback = new(update.FullArchiveUrl, update.ClientIdentifier, string.Empty);
        return DownloadFullUpdate(fallback);
    }

    private string GetOutputDirectory(string clientIdentifier)
    {
        string outputDir = _settings.OutputPath;
        if (string.IsNullOrEmpty(outputDir))
            outputDir = Environment.CurrentDirectory;

        if (this._settings.IncludeClientIdentifierInOutputPath)
            outputDir = Path.Combine(outputDir, clientIdentifier + Path.DirectorySeparatorChar);

        return outputDir;
    }

    private static void SetLinuxExecutableBit(string outputDir)
    {
#if NET10_0_OR_GREATER
        string osuExecutable = Path.Combine(outputDir, "osu!");
        if (OperatingSystem.IsLinux() && File.Exists(osuExecutable))
        {
            UnixFileMode fileMode = File.GetUnixFileMode(osuExecutable);
            fileMode |= UnixFileMode.GroupExecute | UnixFileMode.OtherExecute | UnixFileMode.UserExecute;
            File.SetUnixFileMode(osuExecutable, fileMode);
        }
#endif
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
