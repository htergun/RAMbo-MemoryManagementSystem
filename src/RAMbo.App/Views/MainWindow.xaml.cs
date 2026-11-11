using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RAMbo.App.Data;
using RAMbo.App.Services;

namespace RAMbo.App.Views;

public partial class MainWindow : Window
{
    private readonly SimulationRunner _runner;
    private readonly SimulationRepository _repo;

    public MainWindow(SimulationRunner runner, SimulationRepository repo)
    {
        InitializeComponent();
        _runner = runner;
        _repo = repo;

        ViewPageReplacement.Initialize(_runner);
        ViewPartitioning.Initialize(_runner);
        ViewHistory.Initialize(_repo);

        CheckDatabaseConnection();
    }

    private void CheckDatabaseConnection()
    {
        try
        {
            _ = _repo.GetAll();
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
