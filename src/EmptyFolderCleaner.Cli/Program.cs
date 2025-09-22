using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using EmptyFolderCleaner.Core;

var parseResult = ParseArguments(args);
if (!parseResult.Success)
{
    if (parseResult.ShowHelp)
    {
        PrintUsage();
        return 0;
    }

    Console.Error.WriteLine(parseResult.ErrorMessage);
    PrintUsage();
    return 1;
}

if (parseResult.Configuration is null)
{
    PrintUsage();
    return 0;
}

var configuration = parseResult.Configuration.Value;
if (!Directory.Exists(configuration.Root))
{
    Console.Error.WriteLine($"The directory '{configuration.Root}' does not exist.");
    return 1;
}

var excludePatterns = NormalizeTokens(configuration.ExcludePatterns);
var excludePaths = NormalizeTokens(configuration.ExcludePaths);
var sendToRecycleBin = configuration.Delete && !configuration.Permanent && OperatingSystem.IsWindows();

if (configuration.Delete && !configuration.Permanent && !OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("Recycle Bin deletion is only supported on Windows. Falling back to permanent deletion.");
}

var options = new DirectoryCleanOptions
{
    DryRun = !configuration.Delete,
    SendToRecycleBin = sendToRecycleBin,
    SkipReparsePoints = !configuration.IncludeReparse,
    MaxDepth = configuration.Depth,
    DeleteRootWhenEmpty = configuration.DeleteRoot,
    ExcludedNamePatterns = excludePatterns,
    ExcludedFullPaths = excludePaths
};

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    var result = DirectoryCleaner.Clean(configuration.Root, options, cts.Token);
    if (configuration.Json)
    {
        WriteJson(configuration.Root, options, result);
    }
    else
    {
        WriteHumanReadable(options, result);
    }

    return result.HasFailures ? 2 : 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Operation cancelled.");
    return 130;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static void WriteHumanReadable(DirectoryCleanOptions options, DirectoryCleanResult result)
{
    if (options.DryRun)
    {
        Console.WriteLine($"Found {result.EmptyFound} empty directories (preview mode).");
    }
    else
    {
        Console.WriteLine($"Deleted {result.DeletedCount} of {result.EmptyFound} empty directories.");
    }

    if (result.EmptyDirectories.Count > 0)
    {
        var deletedSet = new HashSet<string>(result.DeletedDirectories, GetPathComparer());
        Console.WriteLine();
        foreach (var path in result.EmptyDirectories)
        {
            var status = options.DryRun ? "preview" : (deletedSet.Contains(path) ? "deleted" : "skipped");
            Console.WriteLine($"- [{status}] {path}");
        }
    }

    if (result.Failures.Count > 0)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("Failures:");
        foreach (var failure in result.Failures)
        {
            Console.Error.WriteLine($"- {failure.Path}: {failure.Exception.Message}");
        }
    }
}

static void WriteJson(string root, DirectoryCleanOptions options, DirectoryCleanResult result)
{
    var payload = new
    {
        root,
        options.DryRun,
        options.SendToRecycleBin,
        options.SkipReparsePoints,
        options.MaxDepth,
        options.DeleteRootWhenEmpty,
        EmptyDirectories = result.EmptyDirectories,
        DeletedDirectories = result.DeletedDirectories,
        Failures = result.Failures.Select(f => new { f.Path, Message = f.Exception.Message }).ToArray()
    };

    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);
}

static ParseOutcome ParseArguments(string[] args)
{
    if (args.Length == 0)
    {
        return ParseOutcome.HelpRequested();
    }

    string? root = null;
    bool delete = false;
    bool permanent = false;
    int? depth = null;
    bool includeReparse = false;
    bool deleteRoot = false;
    bool json = false;
    var excludePatterns = new List<string>();
    var excludePaths = new List<string>();

    for (var index = 0; index < args.Length; index++)
    {
        var token = args[index];
        switch (token)
        {
            case "--help" or "-h" or "/?":
                return ParseOutcome.HelpRequested();
            case "--delete":
                delete = true;
                break;
            case "--permanent":
                permanent = true;
                break;
            case "--include-reparse":
                includeReparse = true;
                break;
            case "--delete-root":
                deleteRoot = true;
                break;
            case "--json":
                json = true;
                break;
            case "--depth":
                if (!TryReadInt(args, ref index, out var parsedDepth, out var depthError))
                {
                    return ParseOutcome.Failure(depthError!);
                }

                depth = parsedDepth;
                break;
            case "--exclude":
                if (!TryReadList(args, ref index, excludePatterns, out var excludeError))
                {
                    return ParseOutcome.Failure(excludeError!);
                }

                break;
            case "--exclude-path":
                if (!TryReadList(args, ref index, excludePaths, out var pathError))
                {
                    return ParseOutcome.Failure(pathError!);
                }

                break;
            default:
                if (token.StartsWith('-'))
                {
                    return ParseOutcome.Failure($"Unknown option '{token}'.");
                }

                if (root is not null)
                {
                    return ParseOutcome.Failure("Only one root directory can be provided.");
                }

                root = token;
                break;
        }
    }

    if (root is null)
    {
        return ParseOutcome.Failure("A root directory must be provided.");
    }

    if (depth is < 0)
    {
        return ParseOutcome.Failure("Depth must be non-negative.");
    }

    var configuration = new CliConfiguration(
        Root: root,
        Delete: delete,
        Permanent: permanent,
        Depth: depth,
        IncludeReparse: includeReparse,
        DeleteRoot: deleteRoot,
        Json: json,
        ExcludePatterns: excludePatterns,
        ExcludePaths: excludePaths);

    return ParseOutcome.Successful(configuration);
}

static bool TryReadInt(string[] args, ref int index, out int value, out string? error)
{
    if (index + 1 >= args.Length)
    {
        value = 0;
        error = "Expected an integer value after '--depth'.";
        return false;
    }

    if (!int.TryParse(args[++index], out value))
    {
        error = "Depth must be an integer value.";
        return false;
    }

    error = null;
    return true;
}

static bool TryReadList(string[] args, ref int index, List<string> target, out string? error)
{
    if (index + 1 >= args.Length)
    {
        error = $"Expected a value after '{args[index]}'.";
        return false;
    }

    var value = args[++index];
    target.AddRange(SplitTokens(value));
    error = null;
    return true;
}

static IReadOnlyCollection<string> NormalizeTokens(IEnumerable<string> tokens)
{
    return tokens
        .Select(token => token.Trim())
        .Where(token => !string.IsNullOrEmpty(token))
        .Distinct(GetPathComparer())
        .ToArray();
}

static IEnumerable<string> SplitTokens(string value)
{
    return value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

static StringComparer GetPathComparer() => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

static void PrintUsage()
{
    Console.WriteLine("Empty Folder Cleaner CLI");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  emptycleaner <root> [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --delete                 Delete empty directories (default is preview).");
    Console.WriteLine("  --permanent              Permanently delete instead of using the Recycle Bin.");
    Console.WriteLine("  --depth <n>              Limit traversal depth relative to the root.");
    Console.WriteLine("  --exclude <pattern>      Wildcard pattern (names or relative paths) to skip; ';' separates multiple values.");
    Console.WriteLine("  --exclude-path <path>    Explicit directory path to skip; ';' separates multiple values.");
    Console.WriteLine("  --include-reparse        Include symbolic links and junctions in the scan.");
    Console.WriteLine("  --delete-root            Allow deleting the root directory when it becomes empty.");
    Console.WriteLine("  --json                   Emit JSON output instead of text.");
    Console.WriteLine("  --help | -h | /?         Show this help message.");
}

readonly record struct CliConfiguration(
    string Root,
    bool Delete,
    bool Permanent,
    int? Depth,
    bool IncludeReparse,
    bool DeleteRoot,
    bool Json,
    List<string> ExcludePatterns,
    List<string> ExcludePaths);

readonly record struct ParseOutcome(bool Success, bool ShowHelp, string? ErrorMessage, CliConfiguration? Configuration)
{
    public static ParseOutcome Successful(CliConfiguration configuration) => new(true, false, null, configuration);
    public static ParseOutcome HelpRequested() => new(true, true, null, null);
    public static ParseOutcome Failure(string error) => new(false, false, error, null);
}
