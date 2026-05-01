using ICSharpCode.SharpZipLib.Zip;

namespace Titanic.Updater;

public static class UpdateManifestValidator
{
    public const int SupportedFormatVersion = 1;
    public const string SupportedPatchAlgorithm = "bsdiff4";

    public static void Validate(UpdateManifest manifest, string clientIdentifier, ZipFile? zip = null)
    {
        if (manifest.FormatVersion != SupportedFormatVersion)
            throw new PatchUpdateException($"Unsupported update manifest format version: {manifest.FormatVersion}");

        if (!string.Equals(manifest.Client, clientIdentifier, StringComparison.OrdinalIgnoreCase))
            throw new PatchUpdateException($"Update manifest is for client '{manifest.Client}', expected '{clientIdentifier}'");

        if (manifest.Actions == null)
            throw new PatchUpdateException("Update manifest has no actions");

        for (int i = 0; i < manifest.Actions.Count; i++)
            ValidateAction(manifest.Actions[i], zip);
    }

    private static void ValidateAction(UpdateAction action, ZipFile? zip)
    {
        if (action == null)
            throw new PatchUpdateException("Update manifest contains a null action");

        string type = action.Type ?? string.Empty;
        switch (type)
        {
            case "replace":
                RequireSource(action, zip);
                RequireDestination(action);
                RequireChecksum(action.Checksum, "checksum", type);
                break;

            case "delete":
                RequireDestination(action);
                break;

            case "store_if_not_exists":
                RequireSource(action, zip);
                RequireDestination(action);
                RequireChecksum(action.Checksum, "checksum", type);
                break;

            case "patch":
                RequireSource(action, zip);
                RequireDestination(action);
                RequireChecksum(action.SourceChecksum, "source_checksum", type);
                RequireChecksum(action.PatchChecksum, "patch_checksum", type);
                RequireChecksum(action.ResultChecksum, "result_checksum", type);

                if (!string.Equals(action.Algorithm, SupportedPatchAlgorithm, StringComparison.OrdinalIgnoreCase))
                    throw new PatchUpdateException($"Unsupported patch algorithm: {action.Algorithm}");
                break;

            default:
                throw new PatchUpdateException($"Unsupported update action type: {action.Type}");
        }
    }

    private static void RequireSource(UpdateAction action, ZipFile? zip)
    {
        UpdatePathUtil.EnsureRelativeSafePath(action.Source, "Source");

        if (zip != null)
        {
            ZipEntry? entry = zip.GetEntry(UpdatePathUtil.NormalizeArchivePath(action.Source));
            if (entry == null || !entry.IsFile)
                throw new PatchUpdateException($"Update archive is missing source entry: {action.Source}");
        }
    }

    private static void RequireDestination(UpdateAction action)
    {
        UpdatePathUtil.EnsureRelativeSafePath(action.Destination, "Destination");
    }

    private static void RequireChecksum(string checksum, string name, string actionType)
    {
        if (string.IsNullOrEmpty(checksum))
            throw new PatchUpdateException($"Action '{actionType}' is missing {name}");
    }
}
