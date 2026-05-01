using System.Text;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

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

    public static UpdateManifest ReadFromFile(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return ReadFromStream(stream);
    }

    public static UpdateManifest ReadFromStream(Stream stream)
    {
        using StreamReader reader = new(stream, Encoding.UTF8);
        return ReadFromJson(reader.ReadToEnd());
    }
}
