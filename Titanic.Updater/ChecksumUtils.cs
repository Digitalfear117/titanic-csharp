using System.Security.Cryptography;
using System.Text;

namespace Titanic.Updater;

public static class ChecksumUtils
{
    public static string ComputeMd5(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return ComputeMd5(stream);
    }

    public static string ComputeMd5(Stream stream)
    {
        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(stream);
        StringBuilder builder = new(hash.Length * 2);

        for (int i = 0; i < hash.Length; i++)
            builder.Append(hash[i].ToString("x2"));

        return builder.ToString();
    }

    public static bool Md5Equals(string? actual, string? expected)
    {
        if (string.IsNullOrEmpty(actual) || string.IsNullOrEmpty(expected))
            return false;

        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }
}
