using System.Windows.Controls;
using PlateBilling.ViewModels;

namespace PlateBilling.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        DataContext = new SettingsViewModel();
    }
}
