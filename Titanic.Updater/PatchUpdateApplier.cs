using Titanic.API;
using Titanic.Helpers.Patching;

namespace Titanic.Updater;

public sealed class PatchUpdateApplier
{
    private readonly UpdateManagerSettings _settings;
    private readonly string _outputDir;
    private readonly string _stagingDir;
    private readonly string? _executablePath;
    private readonly PayloadDownloader _download;
    public delegate byte[] PayloadDownloader(string url);

    public PatchUpdateApplier(UpdateManagerSettings settings, string outputDir, string stagingDir, string? executablePath = null, PayloadDownloader? download = null)
    {
        _settings = settings;
        _outputDir = outputDir;
        _stagingDir = stagingDir;
        _executablePath = executablePath;
        _download = download ?? DownloadWithTitanicApi;
    }

    public void Apply(IList<DownloadedUpdatePart> parts)
    {
        if (parts == null || parts.Count == 0)
            throw new PatchUpdateException("Patch update path has no downloaded parts");

        string backupRoot = Path.Combine(_stagingDir, "_backup_" + Guid.NewGuid().ToString("N"));
        Dictionary<string, string?> backups = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (DownloadedUpdatePart? part in parts)
                ApplyPart(part, backupRoot, backups);

            DeleteDirectoryIfExists(backupRoot);
        }
        catch
        {
            RestoreBackups(backups);
            DeleteDirectoryIfExists(backupRoot);
            throw;
        }
    }

    private void ApplyPart(DownloadedUpdatePart part, string backupRoot, Dictionary<string, string?> backups)
    {
        UpdateManifest manifest = part.Manifest ?? UpdateManifestReader.ReadFromFile(part.Path);
        UpdateManifestValidator.Validate(manifest, part.ClientIdentifier);

        foreach (var action in manifest.Actions)
            ApplyAction(part, manifest, action, backupRoot, backups);

        _settings.PatchUpdateManifestApplied?.Invoke(new ManifestAppliedEvent(part, manifest));
    }

    private void ApplyAction(DownloadedUpdatePart part, UpdateManifest manifest, UpdateAction action, string backupRoot, Dictionary<string, string?> backups)
    {
        switch (action.Type)
        {
            case "replace":
                Replace(part, action, backupRoot, backups);
                break;

            case "delete":
                Delete(action, backupRoot, backups);
                break;

            case "store_if_not_exists":
                StoreIfNotExists(part, action, backupRoot, backups);
                break;

            case "patch":
                Patch(part, manifest, action, backupRoot, backups);
                break;
        }
    }

    private void Replace(DownloadedUpdatePart part, UpdateAction action, string backupRoot, Dictionary<string, string?> backups)
    {
        string destination = GetDestination(action.Destination);
        string? desinationMd5 = File.Exists(destination) ? ChecksumUtils.ComputeMd5(destination) : null;

        if (ChecksumUtils.Md5Equals(desinationMd5, action.Checksum))
            // No need to replace or download
            return;

        string temp = DownloadPayload(part, action.SourceUrlFull, action.Checksum, "replacement");
        BackupDestination(destination, backupRoot, backups);
        MoveFileAndReplace(temp, destination, _settings.ReplaceCurrentExecutable ? _executablePath : null);
    }

    private void Delete(UpdateAction action, string backupRoot, Dictionary<string, string?> backups)
    {
        string destination = GetDestination(action.Destination);
        BackupDestination(destination, backupRoot, backups);

        if (File.Exists(destination))
            MoveExistingFileOutOfTheWay(destination);
    }

    private void StoreIfNotExists(DownloadedUpdatePart part, UpdateAction action, string backupRoot, Dictionary<string, string?> backups)
    {
        string destination = GetDestination(action.Destination);
        if (File.Exists(destination))
            return;

        string temp = DownloadPayload(part, action.SourceUrlFull, action.Checksum, "stored file");

        BackupDestination(destination, backupRoot, backups);
        MoveFileAndReplace(temp, destination, _settings.ReplaceCurrentExecutable ? _executablePath : null);
    }

    private void Patch(DownloadedUpdatePart part, UpdateManifest manifest, UpdateAction action, string backupRoot, Dictionary<string, string?> backups)
    {
        string destination = GetDestination(action.Destination);

        try
        {
            if (!File.Exists(destination))
                throw new PatchUpdateException($"Cannot patch missing destination: {action.Destination}");

            if (ChecksumUtils.Md5Equals(ChecksumUtils.ComputeMd5(destination), action.Checksum))
                // Already patched
                return;

            VerifyFileChecksum(destination, action.SourceChecksum, "patch source");

            string patchFile = DownloadPayload(part, action.SourceUrlPatch, action.PatchChecksum, "patch");

            string result = Path.Combine(_stagingDir, "patched_" + Guid.NewGuid().ToString("N"));
            new BSPatcher().Patch(destination, result, patchFile);
            VerifyFileChecksum(result, action.Checksum, "patch result");

            BackupDestination(destination, backupRoot, backups);
            MoveFileAndReplace(result, destination, _settings.ReplaceCurrentExecutable ? _executablePath : null);

            _settings.PatchUpdateFilePatched?.Invoke(new FilePatchedEvent(part, manifest, action, destination));
        }
        catch (Exception patchException)
        {
            System.Diagnostics.Debug.WriteLine(patchException);

            try
            {
                // Patching the current file did not succeed, so
                // we have to fall back to downloading the entire file
                string temp = DownloadPayload(part, action.SourceUrlFull, action.Checksum, "full patch fallback");
                BackupDestination(destination, backupRoot, backups);
                MoveFileAndReplace(temp, destination, _settings.ReplaceCurrentExecutable ? _executablePath : null);
            }
            catch (Exception fallbackException)
            {
                // we're fucked type shit
                throw new PatchUpdateException(
                    $"Patch action failed for '{action.Destination}' and full payload fallback also failed.",
                    fallbackException
                );
            }
        }
    }

    /// <summary>
    /// Downloads a payload file to a temporary location.
    /// </summary>
    private string DownloadPayload(DownloadedUpdatePart part, string sourceUrl, string expectedChecksum, string label)
    {
        Uri payloadUri = ResolvePayloadUri(part, sourceUrl, label);
        string outputPath = Path.Combine(_stagingDir, "_payload_" + Guid.NewGuid().ToString("N"));

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        byte[] payload = _download(payloadUri.ToString());
        File.WriteAllBytes(outputPath, payload);
        VerifyFileChecksum(outputPath, expectedChecksum, label);

        return outputPath;
    }

    /// <summary>
    /// Resolves the source URL for a payload, handling both absolute and relative URLs.
    /// If the source URL is absolute, it is returned as-is after validation.
    /// If the source URL is relative, it is resolved with the manifest URL.
    /// </summary>
    private Uri ResolvePayloadUri(DownloadedUpdatePart part, string sourceUrl, string label = "<unknown>")
    {
        if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out Uri? uri))
            return ValidatePayloadUri(uri);

        if (string.IsNullOrEmpty(part.ManifestUrl) || !Uri.TryCreate(part.ManifestUrl, UriKind.Absolute, out Uri? manifestUri))
            throw new PatchUpdateException($"Payload '{label}' has a relative URL but the manifest URL is not absolute");

        return ValidatePayloadUri(new Uri(manifestUri, sourceUrl));
    }

    /// <summary>
    /// Validates that the given URI uses an allowed scheme (HTTP or HTTPS).
    /// </summary>
    private static Uri ValidatePayloadUri(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new PatchUpdateException($"Unsupported payload URL scheme: {uri.Scheme}");

        return uri;
    }

    /// <summary>
    /// Ensures that the file at the given path has an MD5 checksum that matches the expected value.
    /// </summary>
    /// <exception cref="PatchUpdateException">
    /// Thrown if the file's checksum does not match the expected value.
    /// </exception>
    private void VerifyFileChecksum(string path, string expected, string label = "file")
    {
        if (!_settings.ValidatePatchUpdateChecksums)
            return;

        string actual = ChecksumUtils.ComputeMd5(path);

        if (!ChecksumUtils.Md5Equals(actual, expected))
            throw new PatchUpdateException($"Invalid {label} checksum for '{path}'. Expected {expected}, got {actual}");
    }

    /// <summary>
    /// Combines the output directory with the destination path.
    /// </summary>
    private string GetDestination(string destination)
    {
        return UpdatePathUtil.CombineSafe(_outputDir, destination);
    }

    /// <summary>
    /// Backs up the existing file at the destination path to a backup location.
    /// If the file does not exist, it records a null backup.
    /// If the file exists, it copies it to a uniquely named backup file and records the path.
    /// </summary>
    private static void BackupDestination(string destination, string backupRoot, Dictionary<string, string?> backups)
    {
        if (backups.ContainsKey(destination))
            return;

        if (!File.Exists(destination))
        {
            backups[destination] = null;
            return;
        }

        string backup = Path.Combine(backupRoot, Guid.NewGuid().ToString("N"));
        string? backupDirectory = Path.GetDirectoryName(backup);
        if (!string.IsNullOrEmpty(backupDirectory))
            Directory.CreateDirectory(backupDirectory);

        File.Copy(destination, backup, true);
        backups[destination] = backup;
    }

    /// <summary>
    /// Restores the backups by copying each backup file back to its original destination.
    /// </summary>
    private static void RestoreBackups(Dictionary<string, string?> backups)
    {
        foreach (KeyValuePair<string, string?> backup in backups)
        {
            try
            {
                string destination = backup.Key;

                if (File.Exists(destination))
                    MoveExistingFileOutOfTheWay(destination);

                if (backup.Value == null)
                    continue;

                string? directory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.Copy(backup.Value, destination, true);
            }
            catch
            {
                // if this fails we're pretty much screwed anyway lmao
            }
        }
    }

    internal static void MoveFileAndReplace(string source, string destination, string? executablePath)
    {
        try
        {
            DoMoveFileAndReplace(source, destination, executablePath);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to replace '{destination}' with '{source}'.", ex);
        }
    }

    private static void DoMoveFileAndReplace(string source, string destination, string? executablePath)
    {
        string? directory = Path.GetDirectoryName(destination);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(destination))
        {
            string destinationFullPath = Path.GetFullPath(destination);
            if (!string.IsNullOrEmpty(executablePath) && string.Equals(destinationFullPath, Path.GetFullPath(executablePath), StringComparison.OrdinalIgnoreCase))
            {
                if (TryReplaceInUseFile(source, destination))
                    return;

                MoveExistingFileOutOfTheWay(destination, CreateMovedAsidePath(destination));
            }
            else
            {
                MoveExistingFileOutOfTheWay(destination);
            }
        }

        File.Move(source, destination);
    }

    /// <summary>
    /// Attempts to replace a file that may be in use by the operating system
    /// (e.g. the currently running executable) using File.Replace.
    /// </summary>
    private static bool TryReplaceInUseFile(string source, string destination)
    {
        string oldPath = CreateMovedAsidePath(destination);

        try
        {
            File.Replace(source, destination, oldPath);
            TryDeleteFile(oldPath);
            return true;
        }
        catch
        {
            TryDeleteFile(oldPath);
            return false;
        }
    }

    /// <summary>
    /// Moves the existing file at the given path to a new location to free up the original path.
    /// Example: "osu!.dll" -> "osu!.dll.old.{guid}"
    /// If the update is successful, the moved file will be deleted.
    /// If the update fails, the moved file can be used to restore the original state.
    /// </summary>
    private static string MoveExistingFileOutOfTheWay(string path, string? preferredPath = null)
    {
        string movePath = preferredPath ?? CreateMovedAsidePath(path);

        if (File.Exists(movePath))
            MoveExistingFileOutOfTheWay(movePath);

        File.Move(path, movePath);

        if (preferredPath == null)
            TryDeleteFile(movePath);

        return movePath;
    }

    private static string CreateMovedAsidePath(string path)
    {
        string candidate;

        do
        {
            candidate = path + ".old." + Guid.NewGuid().ToString("N");
        }
        while (File.Exists(candidate));

        return candidate;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // If windows still has the file open, leaving the renamed file behind
            // is better than failing the update while the original path is free
        }
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }

    private static byte[] DownloadWithTitanicApi(string url)
    {
        using TitanicAPI api = new();
        return api.Download(url);
    }

}
