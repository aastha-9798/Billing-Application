using PlateBilling.Data;
using System.Windows;

namespace PlateBilling;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DatabaseInitializer.Initialize();
    }
}