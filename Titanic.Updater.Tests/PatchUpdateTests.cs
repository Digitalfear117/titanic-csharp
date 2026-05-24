using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using Titanic.API;
using Titanic.API.Models;
using Titanic.API.Requests;

namespace Titanic.Updater.Tests;

public class PatchUpdateTests
{
    private const string ClientIdentifier = "digital";
    private const string CandidateVersion = "5.2.0";
    private const string CandidateStream = "net20-x86";
    private const string CandidateExecutableChecksum = "a651db3fb93e0b1c47db056efb04c1a6";
    private const string NoPatchPathSkipReason = "No patch update path is currently available for the candidate checksum.";

    [SkippableFact]
    public void FindsPatchUpdatePathForCandidate()
    {
        string checksum = ComputeCandidateChecksum();
        Assert.Equal(CandidateExecutableChecksum, checksum);

        ModdedReleaseUpdateModel update = FetchCandidateUpdateOrSkip(checksum);
        SkipIfNoPatchPath(update);

        Assert.NotNull(update.Client);
        Assert.Equal(ClientIdentifier, update.Client.ClientExtension);
        Assert.Equal(CandidateStream, update.Stream);
        Assert.Equal(CandidateVersion, update.SourceRelease.Version);
        Assert.NotNull(update.TargetRelease);
        Assert.NotNull(update.Path);
        Assert.NotEmpty(update.Path);
        Assert.All(GetUsablePathEntries(update), entry => Assert.False(string.IsNullOrEmpty(entry.UpdateUrl)));
    }

    [SkippableFact]
    public void DownloadsPatchUpdateAndValidatesManifest()
    {
        ModdedReleaseUpdateModel updateModel = FetchCandidateUpdateOrSkip();
        SkipIfNoPatchPath(updateModel);

        UpdateInformation update = new(updateModel, ClientIdentifier);
        Assert.True(update.HasPatchUpdatePath);
        Assert.NotEmpty(update.UpdatePath);

        using TempDirectory temp = new();
        UpdateManagerSettings settings = new()
        {
            DataDirectory = Path.Combine(temp.Path, "data"),
            OutputPath = Path.Combine(temp.Path, "install"),
            ReplaceCurrentExecutable = false,
            PreferPatchUpdates = true,
            FallbackToFullArchive = false
        };

        using UpdateManager manager = new(settings);
        DownloadedUpdate downloadedUpdate = manager.DownloadClientUpdate(update);

        Assert.Equal(DownloadedUpdateKind.PatchUpdatePath, downloadedUpdate.Kind);
        Assert.Equal(ClientIdentifier, downloadedUpdate.ClientIdentifier);
        Assert.Equal(update.UpdatePath.Count, downloadedUpdate.Parts.Count);

        Assert.All(downloadedUpdate.Parts, part =>
        {
            Assert.True(File.Exists(part.Path));
            Assert.NotNull(part.Manifest);
            Assert.Equal(ClientIdentifier, part.Manifest.Client);
            Assert.NotNull(part.Manifest.Actions);
            Assert.NotEmpty(part.Manifest.Actions);
            
            // TODO: Skip if we have no osu!.exe patch?
            Assert.Contains(part.Manifest.Actions, action =>
                string.Equals(action.Type, "patch", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(action.Destination, "osu!.exe", StringComparison.OrdinalIgnoreCase));

            UpdateManifestValidator.Validate(part.Manifest, ClientIdentifier);
        });
    }

    [SkippableFact]
    public void AppliesPatchUpdateToCandidate()
    {
        ModdedReleaseUpdateModel updateModel = FetchCandidateUpdateOrSkip();
        SkipIfNoPatchPath(updateModel);

        using TempDirectory temp = new();
        string installDirectory = Path.Combine(temp.Path, "install");
        CopyDirectory(GetCandidateDirectory(), installDirectory);

        bool exitCalled = false;
        List<string> patchedDestinations = new();
        List<string> appliedVersions = new();

        UpdateManagerSettings settings = new()
        {
            DataDirectory = Path.Combine(temp.Path, "data"),
            OutputPath = installDirectory,
            ReplaceCurrentExecutable = false,
            PreferPatchUpdates = true,
            FallbackToFullArchive = false,
            ValidatePatchUpdateChecksums = true,
            Exit = () => exitCalled = true,
            PatchUpdateFilePatched = e => patchedDestinations.Add(Path.GetFileName(e.Destination)),
            PatchUpdateManifestApplied = e => appliedVersions.Add(e.Manifest.To.Version)
        };

        using UpdateManager manager = new(settings);
        UpdateInformation update = new(updateModel, ClientIdentifier);
        DownloadedUpdate downloadedUpdate = manager.DownloadClientUpdate(update);
        manager.InstallClientUpdate(downloadedUpdate); // This is blocking

        Assert.True(exitCalled);
        Assert.Equal(updateModel.TargetRelease.Checksum, ChecksumUtils.ComputeMd5(Path.Combine(installDirectory, "osu!.exe")));

        HashSet<string> patchActionDestinations = downloadedUpdate.Parts
            .SelectMany(part => part.Manifest.Actions)
            .Where(action => string.Equals(action.Type, "patch", StringComparison.OrdinalIgnoreCase))
            .Select(action => Path.GetFileName(action.Destination))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("osu!.exe", patchActionDestinations);
        Assert.All(patchedDestinations, destination => Assert.Contains(destination, patchActionDestinations));

        // A multi-part update can patch the same file more than once
        // Only the last patch checksum for each destination should match the final file
        Dictionary<string, string> latestChecksumByDestination = new(StringComparer.OrdinalIgnoreCase);

        foreach (UpdateAction action in downloadedUpdate.Parts.SelectMany(part => part.Manifest.Actions))
        {
            if (!string.Equals(action.Type, "patch", StringComparison.OrdinalIgnoreCase))
                continue;

            latestChecksumByDestination[action.Destination] = action.Checksum;
        }

        foreach (KeyValuePair<string, string> patch in latestChecksumByDestination)
        {
            string destination = Path.Combine(installDirectory, patch.Key);
            Assert.True(File.Exists(destination), $"Expected patched file to exist: {patch.Key}");
            Assert.Equal(patch.Value, ChecksumUtils.ComputeMd5(destination));
        }

        Assert.Contains(updateModel.TargetRelease.Version, appliedVersions);
    }

    [Fact]
    public void RestoresFilesWhenUpdateFails()
    {
        using TempDirectory temp = new();
        string installDirectory = Path.Combine(temp.Path, "install");
        string stagingDirectory = Path.Combine(temp.Path, "staging");
        Directory.CreateDirectory(installDirectory);
        Directory.CreateDirectory(stagingDirectory);

        string firstPath = Path.Combine(installDirectory, "first.txt");
        string secondPath = Path.Combine(installDirectory, "second.txt");
        File.WriteAllText(firstPath, "before first");
        File.WriteAllText(secondPath, "before second");

        byte[] replacement = Encoding.UTF8.GetBytes("after first");
        UpdateManifest manifest = new()
        {
            FormatVersion = UpdateManifestValidator.SupportedFormatVersion,
            Client = ClientIdentifier,
            From = new UpdateManifestEndpoint
            {
                Version = "1.0.0",
                ExecutableChecksum = ChecksumUtils.ComputeMd5(Encoding.UTF8.GetBytes("before second"))
            },
            To = new UpdateManifestEndpoint
            {
                Version = "1.0.1",
                ExecutableChecksum = ChecksumUtils.ComputeMd5(Encoding.UTF8.GetBytes("after second"))
            },
            Actions =
            [
                new UpdateAction
                {
                    Type = "replace",
                    SourceUrlFull = "https://updates.test/first",
                    Destination = "first.txt",
                    Checksum = ChecksumUtils.ComputeMd5(replacement)
                },
                new UpdateAction
                {
                    Type = "patch",
                    SourceUrlPatch = "https://updates.test/bad-patch",
                    SourceUrlFull = "https://updates.test/bad-full",
                    Destination = "second.txt",
                    SourceChecksum = ChecksumUtils.ComputeMd5(Encoding.UTF8.GetBytes("before second")),
                    PatchChecksum = ChecksumUtils.ComputeMd5(Encoding.UTF8.GetBytes("expected patch")),
                    Checksum = ChecksumUtils.ComputeMd5(Encoding.UTF8.GetBytes("after second")),
                    Algorithm = UpdateManifestValidator.SupportedPatchAlgorithm
                }
            ]
        };

        DownloadedUpdatePart part = new()
        {
            ClientIdentifier = ClientIdentifier,
            Filename = "manifest.json",
            Path = Path.Combine(temp.Path, "manifest.json"),
            Version = "1.0.1",
            ManifestUrl = "https://updates.test/manifest.json",
            Manifest = manifest
        };
        UpdateManagerSettings settings = new()
        {
            ReplaceCurrentExecutable = false,
            ValidatePatchUpdateChecksums = true
        };

        PatchUpdateApplier applier = new(settings, installDirectory, stagingDirectory, download: url =>
        {
            if (url == "https://updates.test/first")
                return replacement;

            return Encoding.UTF8.GetBytes("invalid payload");
        });

        Assert.Throws<PatchUpdateException>(() => applier.Apply([part]));
        Assert.Equal("before first", File.ReadAllText(firstPath));
        Assert.Equal("before second", File.ReadAllText(secondPath));
    }

    private static ModdedReleaseUpdateModel FetchCandidateUpdateOrSkip(string? checksum = null)
    {
        checksum ??= ComputeCandidateChecksum();

        try
        {
            using TitanicAPI api = new();
            GetModdedReleaseUpdateRequest request = new(ClientIdentifier, checksum: checksum);
            return request.BlockingPerform(api);
        }
        catch (Exception ex)
        {
            Skip.If(true, $"{NoPatchPathSkipReason} API request failed: {ex.Message}");
            throw;
        }
    }

    private static void SkipIfNoPatchPath(ModdedReleaseUpdateModel update)
    {
        if (update.TargetRelease == null ||
            update.Path == null ||
            update.Path.Count == 0 ||
            GetUsablePathEntries(update).Count == 0)
        {
            Skip.If(true, NoPatchPathSkipReason);
        }
    }

    private static List<ModdedReleaseEntryModel> GetUsablePathEntries(ModdedReleaseUpdateModel update)
    {
        return update.Path
            .Where(entry => !string.IsNullOrEmpty(entry.UpdateUrl))
            .ToList();
    }

    private static string GetCandidateDirectory()
    {
        return Path.Combine(GetTestDirectory(), "TestCandidate");
    }

    private static string GetTestDirectory([CallerFilePath] string path = "")
    {
        return Path.GetDirectoryName(path)!;
    }

    private static string ComputeCandidateChecksum()
    {
        return ChecksumUtils.ComputeMd5(Path.Combine(GetCandidateDirectory(), "osu!.exe"));
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(destination, relativePath));
        }

        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(source, file);
            string destinationPath = Path.Combine(destination, relativePath);
            string? destinationDirectory = Path.GetDirectoryName(destinationPath);

            if (!string.IsNullOrEmpty(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            File.Copy(file, destinationPath, true);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "updater-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}
