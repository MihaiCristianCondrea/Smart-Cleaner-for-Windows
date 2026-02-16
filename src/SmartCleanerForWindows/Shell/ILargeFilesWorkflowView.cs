using Microsoft.UI.Xaml.Controls;

namespace SmartCleanerForWindows.Shell;

internal interface ILargeFilesWorkflowView
{
    void SetLargeFilesExclusionState(bool hasExclusions);
    void ShowLargeFilesInfoMessage(string message, InfoBarSeverity severity);
}
