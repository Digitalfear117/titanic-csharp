using Titanic.Updater;

namespace Titanic.Updater.CLI;

internal static class Program
{
    private const string DefaultExecutablePath = "osu!.exe";

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                PrintHelp();
                return 0;
            }

            string command = args[0];
            string[] commandArgs = args.Skip(1).ToArray();

            return command switch
            {
                "build" => BuildPatch(commandArgs),
                _ => Fail($"Unknown command: {command}")
            };
        }
        catch (CliException ex)
        {
            Console.Error.WriteLine("error: " + ex.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine("Run `--help` for usage.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("error: " + ex.Message);
            return 1;
        }
    }

    private static int BuildPatch(string[] args)
    {
        ParsedOptions options = ParsedOptions.Parse(args);
        if (options.HasFlag("help") || options.HasFlag("h"))
        {
            PrintBuildPatchHelp();
            return 0;
        }

        string oldDirectory = RequireDirectory(options, "old");
        string newDirectory = RequireDirectory(options, "new");
        string output = RequireValue(options, "output");
        string client = RequireValue(options, "client");
        string fromVersion = RequireValue(options, "from-version");
        string toVersion = RequireValue(options, "to-version");
        string executablePath = options.GetValue("executable") ?? DefaultExecutablePath;

        string fromExecutableChecksum = options.GetValue("from-executable-checksum")
            ?? ComputeOptionalFileChecksum(oldDirectory, executablePath);
        string toExecutableChecksum = options.GetValue("to-executable-checksum")
            ?? ComputeOptionalFileChecksum(newDirectory, executablePath);

        PatchUpdateBuilder builder = new()
        {
            ClientIdentifier = client,
            FromVersion = fromVersion,
            ToVersion = toVersion,
            FromExecutableChecksum = fromExecutableChecksum,
            ToExecutableChecksum = toExecutableChecksum
        };

        foreach (string path in options.GetValues("store-if-not-exists"))
            builder.StoreIfNotExistsPaths.Add(path);

        Console.WriteLine("Building patch update, this may take a while...");
        UpdateManifest manifest = builder.BuildFromDirectories(oldDirectory, newDirectory, output);

        Console.WriteLine($"Created patch update archive: {Path.GetFullPath(output)}");
        Console.WriteLine($"Client: {manifest.Client}");
        Console.WriteLine($"Version: {manifest.From.Version} -> {manifest.To.Version}");
        Console.WriteLine($"Actions: {manifest.Actions.Count}");

        PrintActionCount(manifest, "patch");
        PrintActionCount(manifest, "replace");
        PrintActionCount(manifest, "delete");
        PrintActionCount(manifest, "store_if_not_exists");

        if (string.IsNullOrEmpty(fromExecutableChecksum))
            Console.WriteLine($"From executable checksum: not set ({executablePath} was not found)");
        else
            Console.WriteLine($"From executable checksum: {fromExecutableChecksum}");

        if (string.IsNullOrEmpty(toExecutableChecksum))
            Console.WriteLine($"To executable checksum: not set ({executablePath} was not found)");
        else
            Console.WriteLine($"To executable checksum: {toExecutableChecksum}");

        return 0;
    }

    private static string RequireDirectory(ParsedOptions options, string name)
    {
        string path = RequireValue(options, name);
        if (!Directory.Exists(path))
            throw new CliException($"Directory for --{name} does not exist: {path}");

        return path;
    }

    private static string RequireValue(ParsedOptions options, string name)
    {
        string? value = options.GetValue(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new CliException($"Missing required option --{name}");

        return value;
    }

    private static string ComputeOptionalFileChecksum(string directory, string relativePath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(directory, relativePath));
        string fullRoot = Path.GetFullPath(AppendDirectorySeparator(directory));

        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new CliException($"Executable path escapes its directory: {relativePath}");

        return File.Exists(fullPath) ? ChecksumUtils.ComputeMd5(fullPath) : string.Empty;
    }

    private static string AppendDirectorySeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            return path;

        return path + Path.DirectorySeparatorChar;
    }

    private static void PrintActionCount(UpdateManifest manifest, string type)
    {
        int count = manifest.Actions.Count(action => string.Equals(action.Type, type, StringComparison.OrdinalIgnoreCase));
        Console.WriteLine($"- {type}: {count}");
    }

    private static bool IsHelp(string arg)
    {
        return arg == "--help" || arg == "-h" || arg == "help";
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine("error: " + message);
        Console.Error.WriteLine();
        PrintHelp();
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
        Titanic! Updater CLI

        Usage:
          Titanic.Updater.CLI build [options]

        Commands:
          build - Build a patch update archive from old and new client directories.

        Run `Titanic.Updater.CLI build --help` for command options.
        """);
    }

    private static void PrintBuildPatchHelp()
    {
        Console.WriteLine("""
        Build a patch update archive.

        Usage:
          build --old <dir> --new <dir> --output <zip> --client <id> --from-version <version> --to-version <version> [options]

        Required:
          --old <dir>                    Directory containing the source client version.
          --new <dir>                    Directory containing the target client version.
          --output <zip>                 Patch update archive to create.
          --client <id>                  Client identifier written to update.json.
          --from-version <version>       Source version written to update.json.
          --to-version <version>         Target version written to update.json.

        Options:
          --executable <path>              Relative executable path for automatic MD5 checksums. Default: osu!.exe
          --store-if-not-exists <path>     Mark a target-relative path as store_if_not_exists. Can be repeated.
          --from-executable-checksum <md5> Override source executable checksum.
          --to-executable-checksum <md5>   Override target executable checksum.
          --help                           Show this help text.
        """);
    }

    private sealed class ParsedOptions
    {
        private readonly Dictionary<string, List<string>> _values = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _flags = new(StringComparer.OrdinalIgnoreCase);

        public static ParsedOptions Parse(string[] args)
        {
            ParsedOptions options = new();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (!arg.StartsWith("-", StringComparison.Ordinal))
                    throw new CliException($"Unexpected argument: {arg}");

                string name = TrimOptionPrefix(arg);
                if (string.IsNullOrEmpty(name))
                    throw new CliException("Encountered an empty option name");

                if (name == "help" || name == "h")
                {
                    options._flags.Add(name);
                    continue;
                }

                if (i + 1 >= args.Length || args[i + 1].StartsWith("-", StringComparison.Ordinal))
                    throw new CliException($"Missing value for --{name}");

                string value = args[++i];
                if (!options._values.TryGetValue(name, out List<string>? values))
                {
                    values = new List<string>();
                    options._values[name] = values;
                }

                values.Add(value);
            }

            return options;
        }

        public string? GetValue(string name)
        {
            if (!_values.TryGetValue(name, out List<string>? values) || values.Count == 0)
                return null;

            return values[^1];
        }

        public IEnumerable<string> GetValues(string name)
        {
            return _values.TryGetValue(name, out List<string>? values)
                ? values
                : Enumerable.Empty<string>();
        }

        public bool HasFlag(string name)
        {
            return _flags.Contains(name);
        }

        private static string TrimOptionPrefix(string value)
        {
            if (value.StartsWith("--", StringComparison.Ordinal))
                return value.Substring(2);

            if (value.StartsWith("-", StringComparison.Ordinal))
                return value.Substring(1);

            return value;
        }
    }

    private sealed class CliException(string message) : Exception(message);
}
