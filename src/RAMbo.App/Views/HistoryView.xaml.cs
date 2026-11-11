using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RAMbo.App.Data;
using RAMbo.App.Models;

namespace RAMbo.App.Views;

public partial class HistoryView : UserControl
{
    private SimulationRepository? _repo;

    public HistoryView()
    {
        InitializeComponent();
    }

    public void Initialize(SimulationRepository repo)
    {
        _repo = repo;
        LoadHistory();
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        LoadHistory();
    }

    public void LoadHistory()
    {
        if (_repo == null) return;

        PnlHistoryList.Children.Clear();

        try
        {
            var sims = _repo.GetAll();

            if (sims.Count == 0)
            {
                TxtEmptyMessage.Text = "No saved simulations found. Run an experiment and click 'Save to DB'.";
                TxtEmptyMessage.Visibility = Visibility.Visible;
                return;
            }

            TxtEmptyMessage.Visibility = Visibility.Collapsed;

            foreach (var sim in sims)
            {
                var card = CreateExpanderItem(sim);
                PnlHistoryList.Children.Add(card);
            }
        }
        catch (Exception ex)
        {
            TxtEmptyMessage.Text = $"Database query error:\n{ex.Message}";
            TxtEmptyMessage.Visibility = Visibility.Visible;
        }
    }

    private Expander CreateExpanderItem(Simulation sim)
    {
        var expander = new Expander
        {
            Margin = new Thickness(0, 0, 0, 8),
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10)
        };

        var headerGrid = new Grid { Margin = new Thickness(4, 2, 4, 2) };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titlePanel = new StackPanel { Orientation = Orientation.Horizontal };

        bool isPageRep = sim.SimulationType == "PageReplacement";
        var badge = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(7, 2, 7, 2),
            Margin = new Thickness(0, 0, 10, 0),
            Background = isPageRep 
                ? new SolidColorBrush(Color.FromRgb(219, 234, 254)) 
                : new SolidColorBrush(Color.FromRgb(254, 226, 226))
        };
        var badgeText = new TextBlock
        {
            Text = isPageRep ? "Page Replacement" : "Partitioning",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = isPageRep 
                ? new SolidColorBrush(Color.FromRgb(30, 64, 175)) 
                : new SolidColorBrush(Color.FromRgb(153, 27, 27))
        };
        badge.Child = badgeText;
        titlePanel.Children.Add(badge);

        var txtName = new TextBlock
        {
            Text = sim.Name,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0)
        };
        titlePanel.Children.Add(txtName);

        string summaryText = isPageRep
            ? $"Faults: {sim.TotalPageFaults} | Hits: {sim.TotalHits} | Ratio: {sim.HitRatio:F1}%"
            : $"Internal Frag: {sim.InternalFragmentation} KB | External Frag: {sim.ExternalFragmentation} KB";

        var txtSummary = new TextBlock
        {
            Text = summaryText,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
            VerticalAlignment = VerticalAlignment.Center
        };
        titlePanel.Children.Add(txtSummary);

        Grid.SetColumn(titlePanel, 0);
        headerGrid.Children.Add(titlePanel);

        var rightPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var txtDate = new TextBlock
        {
            Text = sim.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0)
        };
        rightPanel.Children.Add(txtDate);

        var btnDelete = new Button
        {
            Content = "Delete",
            FontSize = 11,
            Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)),
            Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 2, 8, 2),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btnDelete.Click += (s, e) =>
        {
            e.Handled = true;
            if (MessageBox.Show($"Are you sure you want to delete '{sim.Name}'?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    _repo?.Delete(sim.Id);
                    PnlHistoryList.Children.Remove(expander);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Delete error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        };
        rightPanel.Children.Add(btnDelete);

        Grid.SetColumn(rightPanel, 1);
        headerGrid.Children.Add(rightPanel);

        expander.Header = headerGrid;

        bool isLoaded = false;
        var contentPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        expander.Content = contentPanel;

        expander.Expanded += (s, e) =>
        {
            if (isLoaded) return;
            isLoaded = true;

            var fullSim = _repo?.GetById(sim.Id);
            if (fullSim == null)
            {
                contentPanel.Children.Add(new TextBlock { Text = "Detailed record could not be loaded." });
                return;
            }

            if (fullSim.SimulationType == "PageReplacement")
            {
                var grid = new DataGrid
                {
                    AutoGenerateColumns = false,
                    MaxHeight = 280,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                grid.Columns.Add(new DataGridTextColumn { Header = "Step", Binding = new System.Windows.Data.Binding("StepNumber"), Width = 70 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Page", Binding = new System.Windows.Data.Binding("PageNumber"), Width = 90 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Frame State", Binding = new System.Windows.Data.Binding("FrameState"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
                grid.Columns.Add(new DataGridTextColumn { Header = "Status", Binding = new System.Windows.Data.Binding("StatusText"), Width = 90 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Victim Frame", Binding = new System.Windows.Data.Binding("VictimText"), Width = 110 });

                var items = fullSim.Results.OrderBy(r => r.StepNumber).Select(r => new
                {
                    r.StepNumber,
                    r.PageNumber,
                    FrameState = $"[ {r.FrameState} ]",
                    StatusText = r.IsFault ? "FAULT" : "HIT",
                    VictimText = r.VictimPage.HasValue ? $"Page {r.VictimPage.Value}" : "-"
                }).ToList();

                grid.ItemsSource = items;
                contentPanel.Children.Add(grid);
            }
            else
            {
                var grid = new DataGrid
                {
                    AutoGenerateColumns = false,
                    MaxHeight = 280,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                grid.Columns.Add(new DataGridTextColumn { Header = "Process", Binding = new System.Windows.Data.Binding("ProcessName"), Width = 90 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Process Size", Binding = new System.Windows.Data.Binding("ProcessSize"), Width = 100 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Allocated Block", Binding = new System.Windows.Data.Binding("BlockName"), Width = 110 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Block Size", Binding = new System.Windows.Data.Binding("BlockSize"), Width = 100 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Internal Frag", Binding = new System.Windows.Data.Binding("InternalFrag"), Width = 110 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Status", Binding = new System.Windows.Data.Binding("Status"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });

                var procMap = fullSim.Processes.ToDictionary(p => p.Id);
                var blockMap = fullSim.MemoryBlocks.ToDictionary(b => b.Id);

                var items = fullSim.Allocations.Select(a =>
                {
                    var proc = procMap.GetValueOrDefault(a.ProcessId);
                    var block = a.MemoryBlockId.HasValue ? blockMap.GetValueOrDefault(a.MemoryBlockId.Value) : null;
                    return new
                    {
                        ProcessName = proc?.Name ?? "-",
                        ProcessSize = proc != null ? $"{proc.Size} KB" : "-",
                        BlockName = block != null ? $"Block {block.BlockIndex}" : "-",
                        BlockSize = block != null ? $"{block.Size} KB" : "-",
                        InternalFrag = block != null ? $"{a.InternalFragmentation ?? 0} KB" : "-",
                        Status = block != null ? "ALLOCATED" : "NOT ALLOCATED"
                    };
                }).ToList();

                grid.ItemsSource = items;
                contentPanel.Children.Add(grid);
            }
        };

        return expander;
    }
}
