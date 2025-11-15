using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Smart_Cleaner_for_Windows.Core.FileSystem;

namespace Smart_Cleaner_for_Windows.Core.LargeFiles;

public sealed class LargeFileExplorer(IDirectorySystem directorySystem) : ILargeFileExplorer // FIXME: Cannot resolve symbol 'IDirectorySystem'
{
    private static readonly bool IgnoreCase = OperatingSystem.IsWindows();
    private static readonly StringComparer PathComparer = IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private static readonly Dictionary<string, string> ExtensionGroups = new(IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
    {
        [".mp4"] = "Videos",
        [".mkv"] = "Videos",
        [".avi"] = "Videos",
        [".mov"] = "Videos",
        [".wmv"] = "Videos",
        [".mpg"] = "Videos",
        [".mpeg"] = "Videos",
        [".m4v"] = "Videos",
        [".flv"] = "Videos",
        [".3gp"] = "Videos",
        [".mp3"] = "Audio",
        [".wav"] = "Audio",
        [".flac"] = "Audio",
        [".aac"] = "Audio",
        [".ogg"] = "Audio",
        [".wma"] = "Audio",
        [".m4a"] = "Audio",
        [".aif"] = "Audio",
        [".aiff"] = "Audio",
        [".jpg"] = "Pictures",
        [".jpeg"] = "Pictures",
        [".png"] = "Pictures",
        [".gif"] = "Pictures",
        [".bmp"] = "Pictures",
        [".tif"] = "Pictures",
        [".tiff"] = "Pictures",
        [".heic"] = "Pictures",
        [".webp"] = "Pictures",
        [".raw"] = "Pictures",
        [".psd"] = "Creative projects",
        [".ai"] = "Creative projects",
        [".indd"] = "Creative projects",
        [".aep"] = "Creative projects",
        [".prproj"] = "Creative projects",
        [".blend"] = "Creative projects",
        [".zip"] = "Archives",
        [".rar"] = "Archives",
        [".7z"] = "Archives",
        [".gz"] = "Archives",
        [".bz2"] = "Archives",
        [".xz"] = "Archives",
        [".tar"] = "Archives",
        [".iso"] = "Disk images",
        [".img"] = "Disk images",
        [".vhd"] = "Disk images",
        [".vhdx"] = "Disk images",
        [".cab"] = "Archives",
        [".msi"] = "Apps & installers",
        [".msix"] = "Apps & installers",
        [".appx"] = "Apps & installers",
        [".exe"] = "Apps & installers",
        [".bat"] = "Apps & installers",
        [".cmd"] = "Apps & installers",
        [".ps1"] = "Apps & installers",
        [".dll"] = "System files",
        [".sys"] = "System files",
        [".drv"] = "System files",
        [".tmp"] = "Temporary files",
        [".log"] = "Logs",
        [".bak"] = "Backups & databases",
        [".sql"] = "Backups & databases",
        [".db"] = "Backups & databases",
        [".sqlite"] = "Backups & databases",
        [".dbf"] = "Backups & databases",
        [".mdf"] = "Backups & databases",
        [".ldf"] = "Backups & databases",
        [".pdf"] = "Documents",
        [".doc"] = "Documents",
        [".docx"] = "Documents",
        [".rtf"] = "Documents",
        [".txt"] = "Documents",
        [".ppt"] = "Documents",
        [".pptx"] = "Documents",
        [".xls"] = "Documents",
        [".xlsx"] = "Documents",
        [".csv"] = "Documents",
        [".odt"] = "Documents",
        [".odp"] = "Documents",
        [".ods"] = "Documents",
        [".epub"] = "Documents",
        [".xps"] = "Documents",
    };

    private readonly IDirectorySystem _directorySystem = directorySystem ?? throw new ArgumentNullException(nameof(directorySystem)); // FIXME: Cannot resolve symbol 'IDirectorySystem'

    public static ILargeFileExplorer Default { get; } = new LargeFileExplorer();

    private LargeFileExplorer()
        : this(new FileSystemDirectorySystem()) // FIXME: Argument type 'Smart_Cleaner_for_Windows.Core.FileSystem.FileSystemDirectorySystem' is not assignable to parameter type 'IDirectorySystem'
    {
    }

    public LargeFileScanResult Scan(string root, LargeFileScanOptions? options = null, CancellationToken cancellationToken = default)
    {
        return ScanInternal(root, options, cancellationToken);
    }

    public Task<LargeFileScanResult> ScanAsync(string root, LargeFileScanOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ScanInternal(root, options, cancellationToken), cancellationToken);
    }

    private LargeFileScanResult ScanInternal(string root, LargeFileScanOptions? options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentNullException(nameof(root));
        }

        options ??= new LargeFileScanOptions();

        if (!_directorySystem.Exists(root))
        {
            throw new DirectoryNotFoundException($"The directory '{root}' does not exist.");
        }

        var normalizedRoot = NormalizePath(root);
        var files = new List<LargeFileEntry>(); // FIXME: Cannot resolve symbol 'LargeFileEntry'
        var failures = new List<LargeFileScanFailure>();
        var exclusions = new PathExclusionFilter(normalizedRoot, options, failures);

        var pending = new Stack<string>();
        pending.Push(normalizedRoot);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = pending.Pop();

            IEnumerable<string> fileEntries;
            try
            {
                fileEntries = Directory.EnumerateFiles(current);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failures.Add(new LargeFileScanFailure(current, ex));
                continue;
            }

            foreach (var file in fileEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var normalizedFile = NormalizePath(file);

                if (exclusions.ShouldExclude(normalizedFile))
                {
                    continue;
                }

                try
                {
                    var info = new FileInfo(normalizedFile);
                    var type = GetFileType(normalizedFile, info.Extension);
                    files.Add(new LargeFileEntry(normalizedFile, info.Length, type)); // FIXME: Cannot resolve symbol 'LargeFileEntry'
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    failures.Add(new LargeFileScanFailure(normalizedFile, ex));
                }
                catch (Exception ex)
                {
                    failures.Add(new LargeFileScanFailure(normalizedFile, ex));
                }
            }

            if (!options.IncludeSubdirectories)
            {
                continue;
            }

            IEnumerable<string> children;
            try
            {
                children = _directorySystem.EnumerateDirectories(current);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failures.Add(new LargeFileScanFailure(current, ex));
                continue;
            }

            foreach (var child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var normalizedChild = NormalizePath(child);

                if (exclusions.ShouldExclude(normalizedChild))
                {
                    continue;
                }

                if (options.SkipReparsePoints && IsReparsePoint(normalizedChild, failures))
                {
                    continue;
                }

                pending.Push(normalizedChild);
            }
        }

        files.Sort((left, right) => right.Size.CompareTo(left.Size));

        if (options.MaxResults > 0 && files.Count > options.MaxResults)
        {
            files = files.Take(options.MaxResults).ToList();
        }

        return new LargeFileScanResult(files, failures);
    }

    private bool IsReparsePoint(string path, ICollection<LargeFileScanFailure> failures)
    {
        try
        {
            var attributes = _directorySystem.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failures.Add(new LargeFileScanFailure(path, ex));
            return true;
        }
    }

    private static string NormalizePath(string path)
    {
        var full = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(full);
    }

    private static string GetFileType(string path, string extension)
    {
        if (!string.IsNullOrEmpty(extension) && ExtensionGroups.TryGetValue(extension, out var label))
        {
            return label;
        }

        if (string.Equals(extension, ".msixbundle", IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            return "Apps & installers";
        }

        if (string.Equals(extension, ".iso", IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            return "Disk images";
        }

        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        if (directory.Contains(Path.DirectorySeparatorChar + "Windows" + Path.DirectorySeparatorChar, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            return "System files";
        }

        return "Other files";
    }

    private sealed class PathExclusionFilter
    {
        private readonly HashSet<string> _fullPathExclusions;
        private readonly string[] _patterns;
        private readonly string _root;

        public PathExclusionFilter(string root, LargeFileScanOptions options, ICollection<LargeFileScanFailure> failures)
        {
            _root = root;
            _fullPathExclusions = new HashSet<string>(PathComparer);

            if (options.ExcludedFullPaths is { Count: > 0 })
            {
                foreach (var candidate in options.ExcludedFullPaths)
                {
                    if (string.IsNullOrWhiteSpace(candidate))
                    {
                        continue;
                    }

                    try
                    {
                        var resolved = Path.IsPathRooted(candidate)
                            ? NormalizePath(candidate)
                            : NormalizePath(Path.Combine(root, candidate));
                        _fullPathExclusions.Add(resolved);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
                    {
                        failures.Add(new LargeFileScanFailure(candidate, ex));
                    }
                }
            }

            if (options.ExcludedNamePatterns is { Count: > 0 })
            {
                var normalized = new List<string>();
                foreach (var pattern in options.ExcludedNamePatterns)
                {
                    if (string.IsNullOrWhiteSpace(pattern))
                    {
                        continue;
                    }

                    if (TryNormalizePattern(pattern, out var normalizedPattern, out var error))
                    {
                        normalized.Add(normalizedPattern);
                    }
                    else if (error is not null)
                    {
                        failures.Add(new LargeFileScanFailure(pattern, error));
                    }
                }

                _patterns = normalized.Count > 0
                    ? normalized.ToArray()
                    : [];
            }
            else
            {
                _patterns = [];
            }
        }

        public bool ShouldExclude(string path)
        {
            if (_fullPathExclusions.Contains(path))
            {
                return true;
            }

            if (_patterns.Length == 0)
            {
                return false;
            }

            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(name) && MatchesAny(_patterns, name))
            {
                return true;
            }

            var relative = Path.GetRelativePath(_root, path);
            if (!string.IsNullOrEmpty(relative) && relative is not ".")
            {
                var normalized = relative.Replace('\\', '/');
                if (MatchesAny(_patterns, normalized))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryNormalizePattern(string pattern, out string normalized, out Exception? error)
        {
            normalized = pattern.Replace('\\', '/');

            try
            {
                _ = FileSystemName.MatchesSimpleExpression(normalized, string.Empty, IgnoreCase);
                error = null;
                return true;
            }
            catch (ArgumentException ex)
            {
                error = new ArgumentException($"Invalid exclusion pattern '{pattern}'.", ex);
                normalized = string.Empty;
                return false;
            }
        }

        private static bool MatchesAny(string[] patterns, string candidate)
        {
            foreach (var pattern in patterns)
            {
                if (FileSystemName.MatchesSimpleExpression(pattern, candidate, IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
