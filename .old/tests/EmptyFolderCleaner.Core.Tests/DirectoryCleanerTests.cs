namespace EmptyFolderCleaner.Core.Tests;

public sealed class DirectoryCleanerTests
{
    [Fact]
    public void DryRun_identifies_nested_empty_directories()
    {
        using var temp = TemporaryDirectory.Create();
        var inner = Directory.CreateDirectory(Path.Combine(temp.FullPath, "outer", "inner")).FullName;
        var sibling = Directory.CreateDirectory(Path.Combine(temp.FullPath, "sibling")).FullName;
        File.WriteAllText(Path.Combine(sibling, "file.txt"), "content");
        var another = Directory.CreateDirectory(Path.Combine(temp.FullPath, "another")).FullName;

        var result = DirectoryCleaner.Clean(temp.FullPath, DirectoryCleanOptions.Default);

        Assert.Contains(inner, result.EmptyDirectories);
        Assert.Contains(another, result.EmptyDirectories);
        Assert.DoesNotContain(temp.FullPath, result.EmptyDirectories);
        Assert.Empty(result.DeletedDirectories);
    }

    [Fact]
    public void Delete_removes_empty_directories_bottom_up()
    {
        using var temp = TemporaryDirectory.Create();
        var leaf = Directory.CreateDirectory(Path.Combine(temp.FullPath, "parent", "leaf")).FullName;
        var nonEmpty = Directory.CreateDirectory(Path.Combine(temp.FullPath, "non-empty")).FullName;
        File.WriteAllText(Path.Combine(nonEmpty, "data.txt"), "value");

        var options = new DirectoryCleanOptions
        {
            DryRun = false,
            SendToRecycleBin = false
        };

        var result = DirectoryCleaner.Clean(temp.FullPath, options);

        Assert.Contains(leaf, result.DeletedDirectories);
        Assert.DoesNotContain(temp.FullPath, result.DeletedDirectories);
        Assert.True(Directory.Exists(temp.FullPath));
        Assert.False(Directory.Exists(leaf));
        Assert.True(Directory.Exists(nonEmpty));
    }

    [Fact]
    public void Exclude_patterns_are_respected()
    {
        using var temp = TemporaryDirectory.Create();
        var git = Directory.CreateDirectory(Path.Combine(temp.FullPath, ".git")).FullName;
        var build = Directory.CreateDirectory(Path.Combine(temp.FullPath, "build", "intermediate")).FullName;
        var obj = Directory.CreateDirectory(Path.Combine(temp.FullPath, "obj")).FullName;

        var options = DirectoryCleanOptions.Default with
        {
            ExcludedNamePatterns = new[] { ".git", "build/*" }
        };

        var result = DirectoryCleaner.Clean(temp.FullPath, options);

        Assert.DoesNotContain(git, result.EmptyDirectories);
        Assert.DoesNotContain(build, result.EmptyDirectories);
        Assert.Contains(obj, result.EmptyDirectories);
    }

    [Fact]
    public void Reparse_points_are_skipped_by_default()
    {
        if (OperatingSystem.IsWindows())
        {
            // Creating symbolic links requires elevation or developer mode on Windows.
            return;
        }

        using var temp = TemporaryDirectory.Create();
        var target = Directory.CreateDirectory(Path.Combine(temp.FullPath, "target")).FullName;
        File.WriteAllText(Path.Combine(target, "file.txt"), "value");
        var linkPath = Path.Combine(temp.FullPath, "link-to-target");
        Directory.CreateSymbolicLink(linkPath, target);
        var lonely = Directory.CreateDirectory(Path.Combine(temp.FullPath, "lonely")).FullName;

        var result = DirectoryCleaner.Clean(temp.FullPath, DirectoryCleanOptions.Default);

        Assert.Contains(lonely, result.EmptyDirectories);
        Assert.DoesNotContain(linkPath, result.EmptyDirectories);
    }

    [Fact]
    public void Max_depth_limits_traversal()
    {
        using var temp = TemporaryDirectory.Create();
        var level1 = Directory.CreateDirectory(Path.Combine(temp.FullPath, "level1")).FullName;
        var level2 = Directory.CreateDirectory(Path.Combine(level1, "level2")).FullName;
        var peer = Directory.CreateDirectory(Path.Combine(temp.FullPath, "peer")).FullName;

        var options = DirectoryCleanOptions.Default with { MaxDepth = 1 };
        var result = DirectoryCleaner.Clean(temp.FullPath, options);

        Assert.Contains(peer, result.EmptyDirectories);
        Assert.DoesNotContain(level2, result.EmptyDirectories);
    }

    [Fact]
    public void Root_is_not_removed_unless_explicitly_allowed()
    {
        using var temp = TemporaryDirectory.Create();
        var child = Directory.CreateDirectory(Path.Combine(temp.FullPath, "child")).FullName;

        var deleteChildren = new DirectoryCleanOptions
        {
            DryRun = false,
            SendToRecycleBin = false
        };

        var result = DirectoryCleaner.Clean(temp.FullPath, deleteChildren);
        Assert.Contains(child, result.DeletedDirectories);
        Assert.True(Directory.Exists(temp.FullPath));

        var deleteRoot = deleteChildren with { DeleteRootWhenEmpty = true };
        var rootResult = DirectoryCleaner.Clean(temp.FullPath, deleteRoot);

        Assert.Contains(temp.FullPath, rootResult.DeletedDirectories);
        Assert.False(Directory.Exists(temp.FullPath));
    }

    [Fact]
    public void Invalid_exclusion_paths_are_reported()
    {
        using var temp = TemporaryDirectory.Create();
        var target = Directory.CreateDirectory(Path.Combine(temp.FullPath, "target")).FullName;

        var options = DirectoryCleanOptions.Default with
        {
            ExcludedFullPaths = new[] { "\0" }
        };

        var result = DirectoryCleaner.Clean(temp.FullPath, options);

        Assert.Contains(target, result.EmptyDirectories);

        var failure = Assert.Single(result.Failures);
        Assert.Equal("\0", failure.Path);
        Assert.IsType<ArgumentException>(failure.Exception);
    }
}

file sealed class TemporaryDirectory : IDisposable
{
    private TemporaryDirectory(string fullPath)
    {
        FullPath = fullPath;
    }

    public string FullPath { get; }

    public static TemporaryDirectory Create()
    {
        var path = Path.Combine(Path.GetTempPath(), "EmptyFolderCleanerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new TemporaryDirectory(path);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(FullPath))
            {
                Directory.Delete(FullPath, recursive: true);
            }
        }
        catch
        {
            // Ignored on purpose – best effort cleanup for tests.
        }
    }
}
