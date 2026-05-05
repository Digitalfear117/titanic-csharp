namespace Titanic.Updater;

public static class UpdateManifestValidator
{
    public const int SupportedFormatVersion = 2;
    public const string SupportedPatchAlgorithm = "bsdiff4";

    public static void Validate(UpdateManifest manifest, string clientIdentifier)
    {
        if (manifest.FormatVersion != SupportedFormatVersion)
            throw new PatchUpdateException($"Unsupported update manifest format version: {manifest.FormatVersion}");

        if (!string.Equals(manifest.Client, clientIdentifier, StringComparison.OrdinalIgnoreCase))
            throw new PatchUpdateException($"Update manifest is for client '{manifest.Client}', expected '{clientIdentifier}'");

        if (manifest.Actions == null)
            throw new PatchUpdateException("Update manifest has no actions");

        foreach (UpdateAction? action in manifest.Actions)
            ValidateAction(action);
    }

    private static void ValidateAction(UpdateAction action)
    {
        if (action == null)
            throw new PatchUpdateException("Update manifest contains a null action");

        string type = action.Type ?? string.Empty;
        switch (type)
        {
            case "replace":
                RequireSourceUrl(action.SourceUrlFull, "source_url_full", type);
                RequireDestination(action);
                RequireChecksum(action.Checksum, "checksum", type);
                break;

            case "delete":
                RequireDestination(action);
                break;

            case "store_if_not_exists":
                RequireSourceUrl(action.SourceUrlFull, "source_url_full", type);
                RequireDestination(action);
                RequireChecksum(action.Checksum, "checksum", type);
                break;

            case "patch":
                RequireSourceUrl(action.SourceUrlPatch, "source_url_patch", type);
                RequireSourceUrl(action.SourceUrlFull, "source_url_full", type);
                RequireDestination(action);
                RequireChecksum(action.SourceChecksum, "source_checksum", type);
                RequireChecksum(action.PatchChecksum, "patch_checksum", type);
                RequireChecksum(action.Checksum, "checksum", type);

                if (!string.Equals(action.Algorithm, SupportedPatchAlgorithm, StringComparison.OrdinalIgnoreCase))
                    throw new PatchUpdateException($"Unsupported patch algorithm: {action.Algorithm}");
                break;

            default:
                throw new PatchUpdateException($"Unsupported update action type: {action.Type}");
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

    private static void RequireSourceUrl(string sourceUrl, string name, string actionType)
    {
        if (string.IsNullOrEmpty(sourceUrl))
            throw new PatchUpdateException($"Action '{actionType}' is missing {name}");
    }
}
