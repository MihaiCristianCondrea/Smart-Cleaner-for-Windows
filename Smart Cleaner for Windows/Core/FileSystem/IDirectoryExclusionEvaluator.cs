namespace Smart_Cleaner_for_Windows.Core.FileSystem;

internal interface IDirectoryExclusionEvaluator
{
    bool ShouldExclude(string path);
}
