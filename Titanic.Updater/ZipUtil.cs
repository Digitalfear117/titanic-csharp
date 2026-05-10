using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace Titanic.Updater;

public static class ZipUtil
{
    private static string? GetCommonTopLevelDirectory(ZipFile zip)
    {
        string? commonDir = null;

        foreach (ZipEntry entry in zip)
        {
            if (!entry.IsFile)
                continue;

            string name = entry.Name.Replace('\\', '/');
            int slashIndex = name.IndexOf('/');

            if (slashIndex <= 0)
                return null; // file at root = no common dir

            string topLevel = name.Substring(0, slashIndex);

            if (commonDir == null)
                commonDir = topLevel;
            else if (commonDir != topLevel)
                return null; // multiple top-level dirs
        }

        return commonDir;
    }
    
    public static void Extract(string zipPath, string outputDir)
    {
        using FileStream fs = File.OpenRead(zipPath);
        using ZipFile zip = new(fs);

        string? topLevel = GetCommonTopLevelDirectory(zip);

        foreach (ZipEntry entry in zip)
        {
            if (!entry.IsFile)
                continue;

            string name = entry.Name.Replace('\\', '/');

            // Exclude the top-level directory when extracting
            if (topLevel != null)
            {
                if (!name.StartsWith(topLevel + "/"))
                    continue;

                name = name.Substring(topLevel.Length + 1);
            }

            if (name.Length == 0)
                continue;

            string fullPath = UpdatePathUtil.CombineSafe(outputDir, name);
            string? directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using Stream zipStream = zip.GetInputStream(entry);
            using FileStream output = File.Create(fullPath);

            StreamUtils.Copy(zipStream, output, new byte[4096]);
        }
    }
}
