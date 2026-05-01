#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using Titanic.Helpers.Patching;

namespace Titanic.Updater;

public sealed class PatchUpdateBuilder
{
    public string ClientIdentifier { get; set; } = string.Empty;
    public string FromVersion { get; set; } = string.Empty;
    public string ToVersion { get; set; } = string.Empty;
    public string FromExecutableChecksum { get; set; } = string.Empty;
    public string ToExecutableChecksum { get; set; } = string.Empty;
    public List<string> StoreIfNotExistsPaths { get; } = new();

    public PatchUpdateBuildResult BuildFromDirectories(string oldDirectory, string newDirectory, string outputDirectory, string baseUrl)
    {
        Uri baseUri = CreateBaseUri(baseUrl);
        Directory.CreateDirectory(outputDirectory);

        string tempDir = Path.Combine(Path.GetTempPath(), "titanic-patch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            List<PatchUpdatePayload> payloads = new();
            UpdateManifest manifest = CreateManifest();

            string[] oldFiles = Directory.Exists(oldDirectory) ? Directory.GetFiles(oldDirectory, "*", SearchOption.AllDirectories) : [];
            string[] newFiles = Directory.GetFiles(newDirectory, "*", SearchOption.AllDirectories);

            Dictionary<string, string> oldByRelative = new(StringComparer.OrdinalIgnoreCase);
            foreach (string oldFile in oldFiles)
                oldByRelative[UpdatePathUtil.GetRelativePath(oldDirectory, oldFile)] = oldFile;

            Dictionary<string, string> newByRelative = new(StringComparer.OrdinalIgnoreCase);
            foreach (string newFile in newFiles)
                newByRelative[UpdatePathUtil.GetRelativePath(newDirectory, newFile)] = newFile;

            foreach (KeyValuePair<string, string> oldFile in oldByRelative)
            {
                if (!newByRelative.ContainsKey(oldFile.Key))
                {
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
                    AddFileAction(manifest, payloads, outputDirectory, baseUri, storeIfNotExists ? "store_if_not_exists" : "replace", newFile.Key, newFile.Value);
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
                    AddFileAction(manifest, payloads, outputDirectory, baseUri, "store_if_not_exists", newFile.Key, newFile.Value);
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
                    string sourceName = CreatePatchSourceName(oldHash, newHash);

                    PatchUpdatePayload payload = CopyPayload(
                        outputDirectory, baseUri, sourceName,
                        patchPath, ChecksumUtils.ComputeMd5(patchPath)
                    );
                    payloads.Add(payload);
                    manifest.Actions.Add(new UpdateAction
                    {
                        Type = "patch",
                        Source = payload.Source,
                        SourceUrl = payload.Url,
                        Destination = newFile.Key,
                        SourceChecksum = oldHash,
                        PatchChecksum = payload.Checksum,
                        ResultChecksum = newHash,
                        Algorithm = UpdateManifestValidator.SupportedPatchAlgorithm
                    });
                }
                else
                {
                    // Patch is larger than the new file, so we might as well just replace it
                    File.Delete(patchPath);
                    AddFileAction(manifest, payloads, outputDirectory, baseUri, "replace", newFile.Key, newFile.Value);
                }
            }

            string manifestPath = Path.Combine(outputDirectory, UpdateManifestReader.ManifestEntryName);
            WriteManifest(manifestPath, manifest);

            return new PatchUpdateBuildResult
            {
                Manifest = manifest,
                ManifestPath = manifestPath,
                Payloads = payloads
            };
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

    private static void AddFileAction(UpdateManifest manifest, List<PatchUpdatePayload> payloads, string outputDirectory, Uri baseUri, string type, string destination, string sourceFile)
    {
        string checksum = ChecksumUtils.ComputeMd5(sourceFile);
        string sourceName = CreateFileSourceName(checksum);

        PatchUpdatePayload payload = CopyPayload(
            outputDirectory, baseUri, sourceName,
            sourceFile, checksum
        );
        payloads.Add(payload);
        manifest.Actions.Add(new UpdateAction
        {
            Type = type,
            Source = payload.Source,
            SourceUrl = payload.Url,
            Destination = destination,
            Checksum = payload.Checksum
        });
    }

    private static PatchUpdatePayload CopyPayload(string outputDirectory, Uri baseUri, string sourceName, string sourceFile, string checksum)
    {
        UpdatePathUtil.EnsureRelativeSafePath(sourceName, "Source");

        string payloadPath = UpdatePathUtil.CombineSafe(outputDirectory, sourceName);
        string? directory = Path.GetDirectoryName(payloadPath);
        
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.Copy(sourceFile, payloadPath, true);

        return new PatchUpdatePayload
        {
            Source = sourceName,
            Path = payloadPath,
            Url = new Uri(baseUri, Uri.EscapeDataString(sourceName)).ToString(),
            Checksum = checksum,
            Size = new FileInfo(payloadPath).Length
        };
    }

    private static void WriteManifest(string manifestPath, UpdateManifest manifest)
    {
        File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        }));
    }

    private static Uri CreateBaseUri(string baseUrl)
    {
        if (string.IsNullOrEmpty(baseUrl))
            throw new PatchUpdateException("Patch update base URL is required");

        if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
            baseUrl += "/";

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? uri))
            throw new PatchUpdateException($"Invalid patch update base URL: {baseUrl}");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new PatchUpdateException($"Unsupported patch update base URL scheme: {uri.Scheme}");

        return uri;
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

    private static string CreatePatchSourceName(string sourceHash, string destinationHash)
    {
        return $"p_{sourceHash}_{destinationHash}";
    }

    private static string CreateFileSourceName(string hash)
    {
        return $"f_{hash}";
    }
}

#nullable disable

public class PatchUpdateBuildResult
{
    public string ManifestPath { get; set; }
    public UpdateManifest Manifest { get; set; }
    public List<PatchUpdatePayload> Payloads { get; set; }
}

public class PatchUpdatePayload
{
    public string Source { get; set; }
    public string Path { get; set; }
    public string Url { get; set; }
    public string Checksum { get; set; }
    public long Size { get; set; }
}
