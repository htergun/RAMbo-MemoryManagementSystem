using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RAMbo.App.Data;
using RAMbo.App.Services;
using RAMbo.App.Views;

namespace RAMbo.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            MessageBox.Show($"Application Error:\n{e.Exception.Message}\n\nStack:\n{e.Exception.StackTrace}", 
                "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        SimulationRepository? repo = null;

        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var connectionString = config.GetConnectionString("RamboDb");

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                var options = new DbContextOptionsBuilder<RamboDbContext>()
                    .UseSqlServer(connectionString)
                    .Options;

                var db = new RamboDbContext(options);
                repo = new SimulationRepository(db);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Initialization note: {ex.Message}");
        }

        var runner = new SimulationRunner(repo);

        var mainWindow = new MainWindow(runner, repo);
        Current.MainWindow = mainWindow;
        mainWindow.Show();
    }
}
