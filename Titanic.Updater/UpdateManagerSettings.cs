namespace Titanic.Updater;

public class UpdateManagerSettings
{
    /// <summary>
    /// Called after installing an update to signal this version of osu! to close.
    /// </summary>
    public Action? Exit;

    /// <summary>
    /// The data directory to use for staging updates. Default is 'Data/Updater'.
    /// </summary>
    public string DataDirectory = "Data/Updater";
    
    /// <summary>
    /// Should we replace the currently running executable? Disable if writing an external update manager.
    /// </summary>
    public bool ReplaceCurrentExecutable = true;

    /// <summary>
    /// The path we should install updates to. Should be left blank if this is an osu! client.
    /// </summary>
    public string OutputPath = string.Empty;

    /// <summary>
    /// Should we include the client identifier as a subdirectory in the output path? Set to true if writing an external update manager.
    /// </summary>
    public bool IncludeClientIdentifierInOutputPath = false;

    /// <summary>
    /// Prefer update_url patch packages when the API provides an ordered update path.
    /// </summary>
    public bool PreferPatchUpdates = true;

    /// <summary>
    /// Fall back to the target release's full archive if a patch update cannot be installed.
    /// </summary>
    public bool FallbackToFullArchive = true;

    /// <summary>
    /// Validate MD5 checksums declared by patch update manifests.
    /// </summary>
    public bool ValidatePatchUpdateChecksums = true;

    /// <summary>
    /// The value to set ZipConstants.DefaultCodePage to. Leave as default (0 on CoreCLR, null on framework) if this doesn't break your client.
    /// Set to null to disable setting this variable. Set to 0 to use the system's default code page (fixes exceptions on CoreCLR).
    /// </summary>
    /// <remarks>
    /// If your client is on CoreCLR and using SharpZipLib, this is your sign to switch to System.IO.Compression.
    /// </remarks>
    public int? SharpZipLibCodePage =
#if NET10_0_OR_GREATER
        0;
#else
        null;
#endif
}
