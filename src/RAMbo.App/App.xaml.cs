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
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var connectionString = config.GetConnectionString("RamboDb") 
                ?? "Server=localhost;Database=RamboMemoryDb;Trusted_Connection=True;TrustServerCertificate=True;";

            var options = new DbContextOptionsBuilder<RamboDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            var db = new RamboDbContext(options);
            var repo = new SimulationRepository(db);
            var runner = new SimulationRunner(repo);

            var mainWindow = new MainWindow(runner, repo);
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application startup failed:\n{ex.Message}", "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
