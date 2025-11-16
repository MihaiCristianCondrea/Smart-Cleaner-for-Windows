namespace SmartCleanerForWindows.Core.FileSystem;

public interface IDirectoryExclusionEvaluator
{
    bool ShouldExclude(string path);
}
