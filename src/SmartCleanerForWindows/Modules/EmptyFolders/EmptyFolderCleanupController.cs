using System;
using System.Threading;
using System.Threading.Tasks;
using Smart_Cleaner_for_Windows.Core.FileSystem;
using Smart_Cleaner_for_Windows.Modules.EmptyFolders.Contracts;

namespace Smart_Cleaner_for_Windows.Modules.EmptyFolders;

/// <summary>
/// Coordinates the empty folder preview and cleanup workflows, keeping the UI logic isolated
/// behind <see cref="IEmptyFolderCleanupView"/>.
/// </summary>
public sealed class EmptyFolderCleanupController(IDirectoryCleaner directoryCleaner, IEmptyFolderCleanupView view)
{
    private readonly IDirectoryCleaner _directoryCleaner = directoryCleaner ?? throw new ArgumentNullException(nameof(directoryCleaner));
    private readonly IEmptyFolderCleanupView _view = view ?? throw new ArgumentNullException(nameof(view));

    public void HandleInvalidRoot()
    {
        _view.ShowInvalidRootSelection();
    }

    public void DismissInfo()
    {
        _view.DismissInfo();
    }

    public async Task PreviewAsync(string root, DirectoryCleanOptions options, CancellationToken cancellationToken)
    {
        _view.PreparePreview();

        try
        {
            var result = await _directoryCleaner.CleanAsync(root, options, cancellationToken).ConfigureAwait(false);
            _view.ShowPreviewResult(result);
        }
        catch (OperationCanceledException)
        {
            _view.ShowPreviewCancelled();
        }
        catch (Exception ex)
        {
            _view.ShowPreviewError(ex);
        }
        finally
        {
            _view.CompleteOperation();
        }
    }

    public async Task CleanupAsync(string root, DirectoryCleanOptions options, int pendingCount, CancellationToken cancellationToken)
    {
        _view.PrepareCleanup(pendingCount);

        try
        {
            var result = await _directoryCleaner.CleanAsync(root, options, cancellationToken).ConfigureAwait(false);
            _view.ShowCleanupResult(result);
        }
        catch (OperationCanceledException)
        {
            _view.ShowCleanupCancelled();
        }
        catch (Exception ex)
        {
            _view.ShowCleanupError(ex);
        }
        finally
        {
            _view.CompleteOperation();
        }
    }
}
