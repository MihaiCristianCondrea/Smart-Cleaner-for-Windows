namespace Smart_Cleaner_for_Windows.Core.FileSystem;

public interface IDirectoryExclusionEvaluator
{
    bool ShouldExclude(string path);
}
