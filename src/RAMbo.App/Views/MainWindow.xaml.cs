using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RAMbo.App.Data;
using RAMbo.App.Services;

namespace RAMbo.App.Views;

public partial class MainWindow : Window
{
    private readonly SimulationRunner? _runner;
    private readonly SimulationRepository? _repo;

    // Visual Studio XAML Designer için parametresiz constructor (ŞART)
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(SimulationRunner runner, SimulationRepository? repo = null) : this()
    {
        _runner = runner;
        _repo = repo;

        ViewPageReplacement.Initialize(_runner);
        ViewPartitioning.Initialize(_runner);

        if (_repo != null)
        {
            ViewHistory.Initialize(_repo);
        }

        Loaded += (_, _) => CheckDatabaseConnectionAsync();
    }

    private async void CheckDatabaseConnectionAsync()
    {
        if (_repo == null)
        {
            DotDbStatus.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            TxtDbStatus.Text = "Database: Disconnected";
            return;
        }

        try
        {
            // UI thread'ini dondurmamak için arkaplanda test et
            await Task.Run(() => _repo.GetAll());
            DotDbStatus.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            TxtDbStatus.Text = "Database: Connected";
        }
        catch
        {
            DotDbStatus.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            TxtDbStatus.Text = "Database: Disconnected";
        }
    }

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl && MainTabs.SelectedIndex == 2)
        {
            ViewHistory.LoadHistory();
        }
    }
}
