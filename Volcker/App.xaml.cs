using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Volcker.Services;
using Volcker.ViewModels;

namespace Volcker;

public partial class App : Application
{
    public new static App Current => (App)Application.Current;
    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services
        services.AddSingleton<FileService>();
        services.AddSingleton<FirewallService>();
        services.AddSingleton<StateService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }
}
