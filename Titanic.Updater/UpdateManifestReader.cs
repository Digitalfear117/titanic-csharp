using System.Text;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using ICSharpCode.SharpZipLib.Zip;

namespace Titanic.Updater;

public static class UpdateManifestReader
{
    public const string ManifestEntryName = "update.json";

    public static UpdateManifest ReadFromJson(string json)
    {
        UpdateManifest? manifest = JsonConvert.DeserializeObject<UpdateManifest>(json);
        if (manifest == null)
            throw new PatchUpdateException("Update manifest had null content");

        return manifest;
    }

    public static UpdateManifest ReadFromZip(string zipPath)
    {
        using FileStream fs = File.OpenRead(zipPath);
        using ZipFile zip = new(fs);
        return ReadFromZip(zip);
    }

    public static UpdateManifest ReadFromZip(ZipFile zip)
    {
        ZipEntry? entry = zip.GetEntry(ManifestEntryName);
        if (entry == null || !entry.IsFile)
            throw new PatchUpdateException("Patch update is missing update.json");

        using Stream stream = zip.GetInputStream(entry);
        using StreamReader reader = new(stream, Encoding.UTF8);
        return ReadFromJson(reader.ReadToEnd());
    }
}
