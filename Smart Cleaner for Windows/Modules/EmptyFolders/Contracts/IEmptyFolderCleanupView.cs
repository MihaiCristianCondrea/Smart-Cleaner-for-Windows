using System;
using Smart_Cleaner_for_Windows.Core.FileSystem;

namespace Smart_Cleaner_for_Windows.Modules.EmptyFolders.Contracts;

/// <summary>
/// Defines the UI contract required by the empty folder cleanup workflow.
/// </summary>
public interface IEmptyFolderCleanupView
{
    void DismissInfo();

    void ShowInvalidRootSelection();

    void PreparePreview();

    void ShowPreviewResult(DirectoryCleanResult result);

    void ShowPreviewCancelled();

    void ShowPreviewError(Exception exception);

    void PrepareCleanup(int pendingCount);

    void ShowCleanupResult(DirectoryCleanResult result);

    void ShowCleanupCancelled();

    void ShowCleanupError(Exception exception);

    void CompleteOperation();
}
