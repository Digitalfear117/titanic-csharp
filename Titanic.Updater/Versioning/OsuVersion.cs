using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Titanic.Updater.Versioning;

[SuppressMessage("ReSharper", "InconsistentNaming")]
// ReSharper disable once PartialTypeWithSinglePart
public readonly partial struct OsuVersion : IEquatable<OsuVersion>
{
    private const string SemVerStr = "([0-9]).([0-9]).([0-9])";
    
    #if NET10_0_OR_GREATER
    [GeneratedRegex(SemVerStr)] private static partial Regex SemVerGenerated();
    private static Regex _semVer => SemVerGenerated();
    #else
    private static readonly Regex _semVer = new(SemVerStr);
    #endif
    
    public OsuVersion(int major, int minor, int patch)
    {
        this.Major = major;
        this.Minor = minor;
        this.Patch = patch;
    }

    public readonly int Major;
    public readonly int Minor;
    public readonly int Patch;

    public bool IsOlderThan(OsuVersion other)
    {
        if (this.Major < other.Major) return true;
        if (this.Minor < other.Minor) return true;
        if (this.Patch < other.Patch) return true;
        return false;
    }

    public bool Equals(OsuVersion other)
    {
        return this.Major == other.Major && this.Minor == other.Minor && this.Patch == other.Patch;
    }

    public static OsuVersion Parse(string version, OsuVersionKind? archetype)
    {
        switch (archetype)
        {
            case OsuVersionKind.Semver:
            {
                Match match = _semVer.Match(version);
                string major = match.Groups[1].Value;
                string minor = match.Groups[2].Value;
                string patch = match.Groups[3].Value;

                return new OsuVersion(int.Parse(major), int.Parse(minor), int.Parse(patch));
            }
            case OsuVersionKind.BuildNumber:
                return new OsuVersion(int.Parse(version.Substring(1)), 0, 0);
            case OsuVersionKind.BuildDate:
            {
                int major = 0;
                int patch = 0;

                int separatorIndex = version.IndexOf('.');
                if (separatorIndex != -1)
                {
                    major = int.Parse(version.Substring(0, separatorIndex));
                    patch = int.Parse(version.Substring(separatorIndex + 1));
                }
                else
                {
                    major = int.Parse(version);
                }
                
                return new OsuVersion(major, 0, patch);
            }
            default:
            {
                if (version[0] == 'b')
                    return Parse(version, OsuVersionKind.BuildNumber);
                if (CountCharacters(version, '.') == 2)
                    return Parse(version, OsuVersionKind.Semver);
                return Parse(version, OsuVersionKind.BuildDate);
            }
        }
    }

    public override string ToString()
    {
        return $"OsuVersion {this.Major}.{this.Minor}.{this.Patch}";
    }

    private static int CountCharacters(string value, char character)
    {
        int count = 0;

        foreach (char c in value)
        {
            if (c == character)
                count++;
        }

        return count;
    }
}