using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace Titanic.Updater;

public sealed class PatchUpdateApplier
{
    private readonly UpdateManagerSettings _settings;
    private readonly string _outputDir;
    private readonly string _stagingDir;
    private readonly string? _executablePath;

    public PatchUpdateApplier(UpdateManagerSettings settings, string outputDir, string stagingDir, string? executablePath = null)
    {
        _settings = settings;
        _outputDir = outputDir;
        _stagingDir = stagingDir;
        _executablePath = executablePath;
    }

    public void Apply(IList<DownloadedUpdatePart> parts)
    {
        if (parts == null || parts.Count == 0)
            throw new PatchUpdateException("Patch update path has no downloaded parts");

        string backupRoot = Path.Combine(_stagingDir, "_backup_" + Guid.NewGuid().ToString("N"));
        Dictionary<string, string?> backups = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            for (int i = 0; i < parts.Count; i++)
                ApplyPart(parts[i], backupRoot, backups);

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
        using FileStream fs = File.OpenRead(part.Path);
        using ZipFile zip = new(fs);

        UpdateManifest manifest = UpdateManifestReader.ReadFromZip(zip);
        UpdateManifestValidator.Validate(manifest, part.ClientIdentifier, zip);

        for (int i = 0; i < manifest.Actions.Count; i++)
            ApplyAction(zip, manifest.Actions[i], backupRoot, backups);
    }

    private void ApplyAction(ZipFile zip, UpdateAction action, string backupRoot, Dictionary<string, string?> backups)
    {
        switch (action.Type)
        {
            case "replace":
                Replace(zip, action, backupRoot, backups);
                break;

            case "delete":
                Delete(action, backupRoot, backups);
                break;

            case "store_if_not_exists":
                StoreIfNotExists(zip, action, backupRoot, backups);
                break;

            case "patch":
                Patch(zip, action, backupRoot, backups);
                break;
        }
    }

    private void Replace(ZipFile zip, UpdateAction action, string backupRoot, Dictionary<string, string?> backups)
    {
        string temp = ExtractSource(zip, action.Source);
        VerifyFileChecksum(temp, action.Checksum, "replacement");

        string destination = GetDestination(action.Destination);
        BackupDestination(destination, backupRoot, backups);
        MoveFileAndReplace(temp, destination, _settings.ReplaceCurrentExecutable ? _executablePath : null);
    }

    private void Delete(UpdateAction action, string backupRoot, Dictionary<string, string?> backups)
    {
        string destination = GetDestination(action.Destination);
        BackupDestination(destination, backupRoot, backups);

        if (File.Exists(destination))
            File.Delete(destination);
    }

    private void StoreIfNotExists(ZipFile zip, UpdateAction action, string backupRoot, Dictionary<string, string?> backups)
    {
        string destination = GetDestination(action.Destination);
        if (File.Exists(destination))
            return;

        string temp = ExtractSource(zip, action.Source);
        VerifyFileChecksum(temp, action.Checksum, "stored file");

        BackupDestination(destination, backupRoot, backups);
        MoveFileAndReplace(temp, destination, _settings.ReplaceCurrentExecutable ? _executablePath : null);
    }

    private void Patch(ZipFile zip, UpdateAction action, string backupRoot, Dictionary<string, string?> backups)
    {
        string destination = GetDestination(action.Destination);
        if (!File.Exists(destination))
            throw new PatchUpdateException($"Cannot patch missing destination: {action.Destination}");

        VerifyFileChecksum(destination, action.SourceChecksum, "patch source");

        string patchFile = ExtractSource(zip, action.Source);
        VerifyFileChecksum(patchFile, action.PatchChecksum, "patch");

        string result = Path.Combine(_stagingDir, Guid.NewGuid().ToString("N") + ".patched");
        new BSPatcher().Patch(destination, result, patchFile);
        VerifyFileChecksum(result, action.ResultChecksum, "patch result");

        BackupDestination(destination, backupRoot, backups);
        MoveFileAndReplace(result, destination, _settings.ReplaceCurrentExecutable ? _executablePath : null);
    }

    private void VerifyFileChecksum(string path, string expected, string label)
    {
        if (!_settings.ValidatePatchUpdateChecksums)
            return;

        string actual = ChecksumUtils.ComputeMd5(path);

        if (!ChecksumUtils.Md5Equals(actual, expected))
            throw new PatchUpdateException($"Invalid {label} checksum for '{path}'. Expected {expected}, got {actual}");
    }

    /// <summary>
    /// Extracts a source file from the update archive to a temporary location.
    /// </summary>
    private string ExtractSource(ZipFile zip, string source)
    {
        string normalized = UpdatePathUtil.NormalizeArchivePath(source);
        ZipEntry? entry = zip.GetEntry(normalized);
        if (entry == null || !entry.IsFile)
            throw new PatchUpdateException($"Update archive is missing source entry: {source}");

        string outputPath = Path.Combine(_stagingDir, "_part_" + Guid.NewGuid().ToString("N"));
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using Stream zipStream = zip.GetInputStream(entry);
        using FileStream output = File.Create(outputPath);
        StreamUtils.Copy(zipStream, output, new byte[4096]);

        return outputPath;
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
            string destination = backup.Key;

            if (File.Exists(destination))
                File.Delete(destination);

            if (backup.Value == null)
                continue;

            string? directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.Copy(backup.Value, destination, true);
        }
    }

    private static void MoveFileAndReplace(string source, string destination, string? executablePath)
    {
        string? directory = Path.GetDirectoryName(destination);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(destination))
        {
            string destinationFullPath = Path.GetFullPath(destination);
            string executableFullPath = Path.GetFullPath(executablePath);

            if (!string.IsNullOrEmpty(executablePath) && string.Equals(destinationFullPath, executableFullPath, StringComparison.OrdinalIgnoreCase))
            {
                string oldPath = destination + ".old";
                if (File.Exists(oldPath))
                    File.Delete(oldPath);

                File.Move(destination, oldPath);
            }
            else
            {
                File.Delete(destination);
            }
        }

        File.Move(source, destination);
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }
}
