using DesktopIcons.App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopIcons.App.Dialogs;

public sealed partial class NameInputDialog : ContentDialog
{
    public string EnteredName => NameBox.Text.Trim();

    public NameInputDialog(string title, string hint, string initial = "")
    {
        InitializeComponent();
        Title = title;
        HintText.Text = hint;
        NameBox.Text = initial;
        Loaded += (_, _) => NameBox.Focus(FocusState.Programmatic);
        Validate();
    }

    private void NameBox_TextChanged(object sender, TextChangedEventArgs e) => Validate();

    private void Validate()
    {
        var err = LayoutService.ValidateName(NameBox.Text.Trim());
        if (err is null)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            IsPrimaryButtonEnabled = true;
        }
        else
        {
            ErrorText.Text = err;
            ErrorText.Visibility = Visibility.Visible;
            IsPrimaryButtonEnabled = false;
        }
    }
}
