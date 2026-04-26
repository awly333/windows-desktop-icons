using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopIcons.App.Dialogs;

public sealed partial class ConfirmDialog : ContentDialog
{
    public ConfirmDialog(string title, string message, string confirmText, string cancelText = "Cancel", bool destructive = false)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        PrimaryButtonText = confirmText;
        CloseButtonText = cancelText;
        DefaultButton = ContentDialogButton.None;

        var primaryStyleKey = destructive ? "PrimaryDestructiveButton" : "PrimaryButton";
        PrimaryButtonStyle = (Style)Application.Current.Resources[primaryStyleKey];
    }
}
