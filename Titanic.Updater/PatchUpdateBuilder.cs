#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace Titanic.Updater;

public sealed class PatchUpdateBuilder
{
    public string ClientIdentifier { get; set; } = string.Empty;
    public string FromVersion { get; set; } = string.Empty;
    public string ToVersion { get; set; } = string.Empty;
    public string FromExecutableChecksum { get; set; } = string.Empty;
    public string ToExecutableChecksum { get; set; } = string.Empty;
    public List<string> StoreIfNotExistsPaths { get; } = new();

    public UpdateManifest BuildFromDirectories(string oldDirectory, string newDirectory, string outputZipPath)
    {
        // Create a temporary directory for storing patch files
        string tempDir = Path.Combine(Path.GetTempPath(), "titanic-patch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            List<PackageEntry> entries = new();
            UpdateManifest manifest = CreateManifest();

            string[] oldFiles = Directory.Exists(oldDirectory) ? Directory.GetFiles(oldDirectory, "*", SearchOption.AllDirectories) : [];
            string[] newFiles = Directory.GetFiles(newDirectory, "*", SearchOption.AllDirectories);

            // Build dictionaries for quick lookup by relative path
            Dictionary<string, string> oldByRelative = new(StringComparer.OrdinalIgnoreCase);
            foreach (var t in oldFiles)
                oldByRelative[UpdatePathUtil.GetRelativePath(oldDirectory, t)] = t;

            Dictionary<string, string> newByRelative = new(StringComparer.OrdinalIgnoreCase);
            foreach (var t in newFiles)
                newByRelative[UpdatePathUtil.GetRelativePath(newDirectory, t)] = t;

            foreach (KeyValuePair<string, string> oldFile in oldByRelative)
            {
                if (!newByRelative.ContainsKey(oldFile.Key))
                {
                    // Add delete action for files that don't exist in the new version
                    manifest.Actions.Add(new UpdateAction
                    {
                        Type = "delete",
                        Destination = oldFile.Key
                    });
                }
            }

            foreach (KeyValuePair<string, string> newFile in newByRelative)
            {
                bool storeIfNotExists = ContainsPath(StoreIfNotExistsPaths, newFile.Key);

                if (!oldByRelative.TryGetValue(newFile.Key, out string? oldFile))
                {
                    // Old version didn't have this file, so we need to add it
                    AddFileAction(manifest, entries, storeIfNotExists ? "store_if_not_exists" : "replace", newFile.Key, newFile.Value);
                    continue;
                }

                string oldHash = ChecksumUtils.ComputeMd5(oldFile);
                string newHash = ChecksumUtils.ComputeMd5(newFile.Value);
                if (ChecksumUtils.Md5Equals(oldHash, newHash))
                    // Old version is the same as new version, so we can skip it
                    continue;

                if (storeIfNotExists)
                {
                    // The file has changed, but we want to store it only if it doesn't exist on the client
                    AddFileAction(manifest, entries, "store_if_not_exists", newFile.Key, newFile.Value);
                    continue;
                }

                // The file has changed, lets create a patch!
                string patchPath = Path.Combine(tempDir, Guid.NewGuid().ToString("N") + ".patch");
                new BSDiffer().Diff(oldFile, newFile.Value, patchPath);

                FileInfo patchInfo = new(patchPath);
                FileInfo newInfo = new(newFile.Value);

                if (patchInfo.Length < newInfo.Length)
                {
                    // Add patch action if the patch is smaller than the new file
                    string sourceName = Guid.NewGuid().ToString("N");
                    entries.Add(new PackageEntry(sourceName, patchPath));
                    manifest.Actions.Add(new UpdateAction
                    {
                        Type = "patch",
                        Source = sourceName,
                        Destination = newFile.Key,
                        SourceChecksum = oldHash,
                        PatchChecksum = ChecksumUtils.ComputeMd5(patchPath),
                        ResultChecksum = newHash,
                        Algorithm = UpdateManifestValidator.SupportedPatchAlgorithm
                    });
                }
                else
                {
                    // Patch is larger than the new file, so we might as well just replace it
                    File.Delete(patchPath);
                    AddFileAction(manifest, entries, "replace", newFile.Key, newFile.Value);
                }
            }

            WritePackage(outputZipPath, manifest, entries);
            return manifest;
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    private UpdateManifest CreateManifest()
    {
        return new UpdateManifest
        {
            FormatVersion = UpdateManifestValidator.SupportedFormatVersion,
            Client = ClientIdentifier,
            From = new UpdateManifestEndpoint
            {
                Version = FromVersion,
                ExecutableChecksum = FromExecutableChecksum
            },
            To = new UpdateManifestEndpoint
            {
                Version = ToVersion,
                ExecutableChecksum = ToExecutableChecksum
            },
            Actions = new List<UpdateAction>()
        };
    }

    private static void AddFileAction(UpdateManifest manifest, List<PackageEntry> entries, string type, string destination, string sourceFile)
    {
        string sourceName = Guid.NewGuid().ToString("N");
        entries.Add(new PackageEntry(sourceName, sourceFile));
        manifest.Actions.Add(new UpdateAction
        {
            Type = type,
            Source = sourceName,
            Destination = destination,
            Checksum = ChecksumUtils.ComputeMd5(sourceFile)
        });
    }

    private static void WritePackage(string outputZipPath, UpdateManifest manifest, List<PackageEntry> entries)
    {
        string? directory = Path.GetDirectoryName(outputZipPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using FileStream fs = File.Create(outputZipPath);
        using ZipOutputStream zip = new(fs);
        zip.SetLevel(9);

        WriteStringEntry(zip, UpdateManifestReader.ManifestEntryName, JsonConvert.SerializeObject(manifest, Formatting.Indented));

        for (int i = 0; i < entries.Count; i++)
            WriteFileEntry(zip, entries[i].EntryName, entries[i].Path);
    }

    private static void WriteStringEntry(ZipOutputStream zip, string name, string content)
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes(content);
        ZipEntry entry = new(name)
        {
            Size = data.Length
        };
        zip.PutNextEntry(entry);
        zip.Write(data, 0, data.Length);
        zip.CloseEntry();
    }

    private static void WriteFileEntry(ZipOutputStream zip, string name, string path)
    {
        FileInfo file = new(path);
        ZipEntry entry = new(name)
        {
            Size = file.Length
        };
        zip.PutNextEntry(entry);

        using FileStream input = File.OpenRead(path);
        StreamUtils.Copy(input, zip, new byte[4096]);
        zip.CloseEntry();
    }

    private static bool ContainsPath(List<string> paths, string path)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            if (string.Equals(UpdatePathUtil.NormalizeArchivePath(paths[i]), path, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private sealed class PackageEntry
    {
        public readonly string EntryName;
        public readonly string Path;

        public PackageEntry(string entryName, string path)
        {
            EntryName = entryName;
            Path = path;
        }
    }
}
