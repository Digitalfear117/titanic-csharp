namespace Titanic.Updater;

internal static class UpdatePathUtil
{
    /// <summary>
    /// This converts backslashes to forward slashes and removes any leading slashes.
    /// </summary>
    public static string NormalizeArchivePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    /// <summary>
    /// Ensures that the given path is a relative path that does not contain any ".." segments.
    /// </summary>
    public static void EnsureRelativeSafePath(string path, string label)
    {
        if (string.IsNullOrEmpty(path))
            throw new PatchUpdateException($"{label} path is empty");

        string normalized = path.Replace('\\', '/');
        if (Path.IsPathRooted(normalized))
            throw new PatchUpdateException($"{label} path is rooted: {path}");

        string[] parts = normalized.Split('/');
        foreach (string part in parts)
        {
            if (part == "..")
                throw new PatchUpdateException($"{label} path escapes the update root: {path}");
        }
    }

    /// <summary>
    /// Combines a root path with a relative path, ensuring the result is safe.
    /// </summary>
    public static string CombineSafe(string root, string relativePath)
    {
        EnsureRelativeSafePath(relativePath, "Destination");

        string fullRoot = Path.GetFullPath(AppendDirectorySeparator(root));
        string fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));

        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new PatchUpdateException($"Destination path escapes output directory: {relativePath}");

        return fullPath;
    }

    /// <summary>
    /// Returns the relative path from the root to the full path, ensuring it is safe.
    /// Example: GetRelativePath("C:\\", "C:\\folder\\file.txt") -> "folder/file.txt"
    /// </summary>
    public static string GetRelativePath(string root, string path)
    {
        string fullRoot = Path.GetFullPath(AppendDirectorySeparator(root));
        string fullPath = Path.GetFullPath(path);

        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new PatchUpdateException($"Path escapes root: {path}");

        return fullPath.Substring(fullRoot.Length).Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>
    /// Appends a directory separator to the end of the path if it doesn't already have one.
    /// </summary>
    private static string AppendDirectorySeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar.ToString()) || path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            return path;

        return path + Path.DirectorySeparatorChar;
    }
}
