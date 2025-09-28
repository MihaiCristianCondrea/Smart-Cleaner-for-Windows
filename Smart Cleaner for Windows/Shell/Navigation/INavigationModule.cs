using System;
using Microsoft.UI.Xaml.Controls;

namespace Smart_Cleaner_for_Windows.Shell.Navigation;

public interface INavigationModule
{
    string Id { get; }
    string Title { get; }
    Symbol Icon { get; }
    int Order { get; }
    Type ViewType { get; }
}
