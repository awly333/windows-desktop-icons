using CommunityToolkit.Mvvm.ComponentModel;
using DesktopIcons.Core.Models;
using DesktopIcons.Core.Storage;
using Microsoft.UI.Xaml;

namespace DesktopIcons.App.ViewModels;

public sealed partial class LayoutItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fingerprint = string.Empty;

    [ObservableProperty]
    private LayoutFile? _layout;

    [ObservableProperty]
    private bool _isExpanded;

    public int IconCount => Layout?.Icons.Count ?? 0;

    public DateTime CapturedAtLocal => (Layout?.CapturedAt ?? default).ToLocalTime();

    public string CapturedAtDisplay =>
        Layout?.CapturedAt is { } d && d != default
            ? d.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : "unknown";

    public Visibility ExpandedVisibility => IsExpanded ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CollapsedVisibility => IsExpanded ? Visibility.Collapsed : Visibility.Visible;

    public static LayoutItemViewModel From(LayoutStore.StoredLayout stored, string fingerprint) =>
        new() { Name = stored.Name, Layout = stored.Layout, Fingerprint = fingerprint };

    partial void OnLayoutChanged(LayoutFile? value)
    {
        OnPropertyChanged(nameof(IconCount));
        OnPropertyChanged(nameof(CapturedAtLocal));
        OnPropertyChanged(nameof(CapturedAtDisplay));
    }

    partial void OnIsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(ExpandedVisibility));
        OnPropertyChanged(nameof(CollapsedVisibility));
    }
}
