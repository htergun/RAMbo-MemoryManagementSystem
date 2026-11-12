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

            var configConn = config.GetConnectionString("RamboDb");

            // Denenecek olası SQL Server sunucu adresleri
            var candidateConnections = new List<string>();

            if (!string.IsNullOrWhiteSpace(configConn))
            {
                candidateConnections.Add(configConn);
            }

            candidateConnections.Add("Server=(localdb)\\MSSQLLocalDB;Database=RamboMemoryDb;Trusted_Connection=True;TrustServerCertificate=True;");
            candidateConnections.Add("Server=localhost;Database=RamboMemoryDb;Trusted_Connection=True;TrustServerCertificate=True;");
            candidateConnections.Add("Server=.\\SQLEXPRESS;Database=RamboMemoryDb;Trusted_Connection=True;TrustServerCertificate=True;");

            foreach (var connStr in candidateConnections)
            {
                try
                {
                    var options = new DbContextOptionsBuilder<RamboDbContext>()
                        .UseSqlServer(connStr, sqlOptions => sqlOptions.CommandTimeout(5))
                        .Options;

                    var db = new RamboDbContext(options);

                    // Veritabanı ve tablolar yoksa otomatik olarak oluşturur!
                    db.Database.EnsureCreated();

                    if (db.Database.CanConnect())
                    {
                        repo = new SimulationRepository(db);
                        break; // Başarılı bağlantı sağlandı
                    }
                }
                catch
                {
                    // Sıradaki sunucu adayını dene
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Auto-Setup Note: {ex.Message}");
        }

        var runner = new SimulationRunner(repo);
        var mainWindow = new MainWindow(runner, repo);
        Current.MainWindow = mainWindow;
        mainWindow.Show();
    }
}
