using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopIcons.App.Services;
using Microsoft.UI.Xaml;

namespace DesktopIcons.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly LayoutService _service = new();
    public LayoutService Service => _service;

    public ObservableCollection<LayoutItemViewModel> Layouts { get; } = new();

    [ObservableProperty]
    private string _currentFingerprint = "";

    [ObservableProperty]
    private string _currentSetup = "";

    [ObservableProperty]
    private string _displaySummary = "";

    [ObservableProperty]
    private LayoutItemViewModel? _selectedLayout;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _showAllFingerprints;

    [ObservableProperty]
    private int _otherFingerprintCount;

    [ObservableProperty]
    private string _toastMessage = "";

    [ObservableProperty]
    private Visibility _toastVisibility = Visibility.Collapsed;

    public event EventHandler<(string Fingerprint, string Name)>? LayoutRestored;

    public bool HasLayouts => Layouts.Count > 0;
    public Visibility LayoutsVisibility => HasLayouts ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EmptyStateVisibility => HasLayouts ? Visibility.Collapsed : Visibility.Visible;

    public MainViewModel()
    {
        Layouts.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasLayouts));
            OnPropertyChanged(nameof(LayoutsVisibility));
            OnPropertyChanged(nameof(EmptyStateVisibility));
        };
    }

    public async Task InitializeAsync()
    {
        var fp = await _service.GetCurrentFingerprintAsync();
        CurrentFingerprint = fp.Fingerprint;
        CurrentSetup = fp.Setup;
        DisplaySummary = fp.DisplaySummary;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            var previouslySelected = SelectedLayout?.Name;
            Layouts.Clear();

            if (ShowAllFingerprints)
            {
                var groups = await _service.ListAllAsync();
                foreach (var g in groups)
                    foreach (var l in g.Layouts)
                        Layouts.Add(LayoutItemViewModel.From(l, g.Fingerprint));
                OtherFingerprintCount = 0;
            }
            else
            {
                var layouts = await _service.ListForFingerprintAsync(CurrentFingerprint);
                foreach (var l in layouts)
                    Layouts.Add(LayoutItemViewModel.From(l, CurrentFingerprint));

                var all = await _service.ListAllAsync();
                OtherFingerprintCount = all
                    .Where(g => g.Fingerprint != CurrentFingerprint)
                    .Sum(g => g.Layouts.Count);
            }

            var sorted = Layouts.OrderByDescending(l => l.CapturedAtLocal).ToList();
            Layouts.Clear();
            foreach (var l in sorted) Layouts.Add(l);

            if (previouslySelected != null)
                SelectedLayout = Layouts.FirstOrDefault(l => l.Name == previouslySelected);
        }
        catch { /* RefreshAsync never throws into the UI */ }
    }

    [RelayCommand]
    private async Task SaveNew(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        IsBusy = true;
        try
        {
            var layout = await _service.SaveCurrentAsync(name);
            LayoutRestored?.Invoke(this, (CurrentFingerprint, name));
            await RefreshAsync();
            SelectedLayout = Layouts.FirstOrDefault(l => l.Name == name);
            if (SelectedLayout != null) SelectedLayout.IsExpanded = true;
            ShowToast($"Saved {layout.Icons.Count} icons");
        }
        catch (Exception ex)
        {
            ShowToast($"Save failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Restore(LayoutItemViewModel item)
    {
        if (item is null) return;
        IsBusy = true;
        try
        {
            var result = await _service.RestoreAsync(item.Fingerprint, item.Name);
            LayoutRestored?.Invoke(this, (item.Fingerprint, item.Name));
            if (result.AutoArrangeOn)
            {
                ShowToast("Auto Arrange is on — positions are ignored. Right-click desktop → View → uncheck Auto arrange icons.");
            }
            else if (result.NotFound > 0)
            {
                ShowToast($"Restored {result.Moved} of {result.Total} icons ({result.NotFound} not found on desktop)");
            }
            else
            {
                ShowToast($"Restored {result.Moved} icons");
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Restore failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Delete(LayoutItemViewModel item)
    {
        if (item is null) return;
        IsBusy = true;
        try
        {
            await _service.DeleteAsync(item.Fingerprint, item.Name);
            await RefreshAsync();
            ShowToast($"Deleted “{item.Name}”");
        }
        catch (Exception ex)
        {
            ShowToast($"Delete failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Rename((LayoutItemViewModel Item, string NewName) args)
    {
        if (args.Item is null || string.IsNullOrWhiteSpace(args.NewName)) return;
        if (string.Equals(args.Item.Name, args.NewName, StringComparison.Ordinal)) return;
        IsBusy = true;
        try
        {
            await _service.RenameAsync(args.Item.Fingerprint, args.Item.Name, args.NewName);
            await RefreshAsync();
            SelectedLayout = Layouts.FirstOrDefault(l => l.Name == args.NewName);
            ShowToast("Renamed");
        }
        catch (Exception ex)
        {
            ShowToast($"Rename failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnShowAllFingerprintsChanged(bool value)
    {
        _ = RefreshAsync();
    }

    private CancellationTokenSource? _toastCts;

    private async void ShowToast(string message)
    {
        ToastMessage = message;
        ToastVisibility = Visibility.Visible;

        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        var token = _toastCts.Token;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2.5), token);
            if (!token.IsCancellationRequested) ToastVisibility = Visibility.Collapsed;
        }
        catch (TaskCanceledException) { }
    }
}
