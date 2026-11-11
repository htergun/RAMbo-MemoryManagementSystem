using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using RAMbo.App.Models;
using RAMbo.App.Services;
using RAMbo.App.Services.Algorithms;

namespace RAMbo.App.Views;

public partial class PartitioningView : UserControl
{
    private SimulationRunner? _runner;
    private PartitionResult? _lastResult;
    private List<MemoryBlock> _lastBlocks = new();
    private List<ProcessInfo> _lastProcesses = new();
    private string _lastMode = "Fixed";

    private static readonly (string Name, string Blocks, string Procs)[] Presets =
    {
        ("— Select Preset Scenario —", "", ""),
        ("Standard Scenario (5 Blocks [100,500,200,300,600], 4 Processes [212,417,112,426])", "100, 500, 200, 300, 600", "212, 417, 112, 426"),
        ("Tight Memory Scenario (4 Blocks [50,100,150,200], 4 Processes [45,80,120,30])", "50, 100, 150, 200", "45, 80, 120, 30"),
        ("Large Memory Scenario (5 Blocks [256,512,128,1024,64], 4 Processes [200,500,100,900])", "256, 512, 128, 1024, 64", "200, 500, 100, 900")
    };

    public PartitioningView()
    {
        InitializeComponent();
        InitializePresets();
    }

    public void Initialize(SimulationRunner runner)
    {
        _runner = runner;
    }

    private void InitializePresets()
    {
        CboPresets.Items.Clear();
        foreach (var p in Presets)
        {
            CboPresets.Items.Add(p.Name);
        }
        CboPresets.SelectedIndex = 0;
    }

    private void CboPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int idx = CboPresets.SelectedIndex;
        if (idx <= 0 || idx >= Presets.Length) return;

        var preset = Presets[idx];
        TxtBlocks.Text = preset.Blocks;
        TxtProcesses.Text = preset.Procs;
    }

    private void BtnRun_Click(object sender, RoutedEventArgs e)
    {
        if (_runner == null) return;

        if (!TryParseInputs(out var blocks, out var processes))
            return;

        PartitionAlgorithmBase algo = CboAlgorithm.SelectedIndex switch
        {
            1 => new BestFit(),
            2 => new WorstFit(),
            _ => new FirstFit()
        };

        var mode = RbFixed.IsChecked == true ? PartitionMode.Fixed : PartitionMode.Variable;
        algo.Mode = mode;

        try
        {
            _lastResult = _runner.RunPartitioning(algo, blocks, processes);
            _lastBlocks = blocks;
            _lastProcesses = processes;
            _lastMode = mode.ToString();

            DisplayResult(_lastResult, blocks, processes, mode.ToString());

            BtnSave.IsEnabled = true;
            BtnExport.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to execute partitioning:\n{ex.Message}", "Execution Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DisplayResult(PartitionResult result, List<MemoryBlock> blocks, List<ProcessInfo> processes, string mode)
    {
        TxtResAlgoMod.Text = $"{result.AlgorithmName} ({mode})";
        TxtResIntFrag.Text = $"{result.InternalFragmentation} KB";
        TxtResExtFrag.Text = $"{result.ExternalFragmentation} KB";
        int failedCount = result.Allocations.Count(a => a.MemoryBlockId == null);
        TxtResFailed.Text = failedCount.ToString();

        var blockMap = blocks.ToDictionary(b => b.Id);
        var procMap = processes.ToDictionary(p => p.Id);

        var viewModels = result.Allocations.Select(a =>
        {
            var proc = procMap.GetValueOrDefault(a.ProcessId);
            var block = a.MemoryBlockId.HasValue ? blockMap.GetValueOrDefault(a.MemoryBlockId.Value) : null;
            bool isAllocated = block != null;

            return new AllocationViewModel
            {
                ProcessName = proc?.Name ?? $"P{a.ProcessId}",
                ProcessSize = proc != null ? $"{proc.Size} KB" : "-",
                BlockName = block != null ? $"Block {block.BlockIndex}" : "-",
                BlockSize = block != null ? $"{block.Size} KB" : "-",
                InternalFragText = isAllocated ? $"{a.InternalFragmentation ?? 0} KB" : "-",
                IsAllocated = isAllocated,
                StatusText = isAllocated ? "ALLOCATED" : "NOT ALLOCATED (Insufficient Space)"
            };
        }).ToList();

        GridAllocations.ItemsSource = viewModels;
        ResultsContainer.Visibility = Visibility.Visible;
    }

    private void GridAllocations_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is AllocationViewModel vm && !vm.IsAllocated)
        {
            e.Row.Background = new SolidColorBrush(Color.FromArgb(35, 239, 68, 68));
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (_runner == null || _lastResult == null) return;

        try
        {
            PartitionAlgorithmBase algo = CboAlgorithm.SelectedIndex switch
            {
                1 => new BestFit(),
                2 => new WorstFit(),
                _ => new FirstFit()
            };
            algo.Mode = _lastMode == "Fixed" ? PartitionMode.Fixed : PartitionMode.Variable;

            _runner.RunPartitioningAndSave(algo, _lastBlocks, _lastProcesses, _lastMode);
            MessageBox.Show("Partitioning simulation successfully saved to database.", "Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save record:\n{ex.Message}", "Database Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        if (_lastResult == null) return;

        var dlg = new SaveFileDialog
        {
            Filter = "Text File (*.txt)|*.txt|CSV File (*.csv)|*.csv",
            FileName = $"{_lastResult.AlgorithmName}_{_lastMode}_Partitioning_{DateTime.Now:yyyyMMdd_HHmm}"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                using var sw = new StreamWriter(dlg.FileName);
                bool isCsv = dlg.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

                var blockMap = _lastBlocks.ToDictionary(b => b.Id);
                var procMap = _lastProcesses.ToDictionary(p => p.Id);

                if (isCsv)
                {
                    sw.WriteLine("Process,ProcessSize,AllocatedBlock,BlockSize,InternalFrag,Status");
                    foreach (var a in _lastResult.Allocations)
                    {
                        var proc = procMap.GetValueOrDefault(a.ProcessId);
                        var block = a.MemoryBlockId.HasValue ? blockMap.GetValueOrDefault(a.MemoryBlockId.Value) : null;
                        sw.WriteLine($"{proc?.Name ?? "-"},{proc?.Size ?? 0},{(block != null ? $"Block {block.BlockIndex}" : "-")},{block?.Size ?? 0},{a.InternalFragmentation ?? 0},{(block != null ? "ALLOCATED" : "NOT ALLOCATED")}");
                    }
                    sw.WriteLine();
                    sw.WriteLine($"Algorithm,{_lastResult.AlgorithmName}");
                    sw.WriteLine($"Mode,{_lastMode}");
                    sw.WriteLine($"InternalFragmentation,{_lastResult.InternalFragmentation}");
                    sw.WriteLine($"ExternalFragmentation,{_lastResult.ExternalFragmentation}");
                }
                else
                {
                    sw.WriteLine("====================================================");
                    sw.WriteLine("RAMbo - Memory Partitioning Report");
                    sw.WriteLine($"Algorithm : {_lastResult.AlgorithmName} | Mode: {_lastMode}");
                    sw.WriteLine($"Blocks    : [{string.Join(", ", _lastBlocks.Select(b => $"{b.Size}KB"))}]");
                    sw.WriteLine($"Processes : [{string.Join(", ", _lastProcesses.Select(p => $"{p.Name}:{p.Size}KB"))}]");
                    sw.WriteLine("====================================================");
                    sw.WriteLine(string.Format("{0,-10} | {1,-12} | {2,-14} | {3,-12} | {4,-14} | {5,-16}", "Process", "Proc Size", "Target Block", "Block Size", "Internal Frag", "Status"));
                    sw.WriteLine(new string('-', 85));
                    foreach (var a in _lastResult.Allocations)
                    {
                        var proc = procMap.GetValueOrDefault(a.ProcessId);
                        var block = a.MemoryBlockId.HasValue ? blockMap.GetValueOrDefault(a.MemoryBlockId.Value) : null;
                        sw.WriteLine(string.Format("{0,-10} | {1,-12} | {2,-14} | {3,-12} | {4,-14} | {5,-16}",
                            proc?.Name ?? "-", $"{proc?.Size} KB", block != null ? $"Block {block.BlockIndex}" : "-", block != null ? $"{block.Size} KB" : "-", $"{a.InternalFragmentation ?? 0} KB", block != null ? "ALLOCATED" : "NOT ALLOCATED"));
                    }
                    sw.WriteLine("----------------------------------------------------");
                    sw.WriteLine($"Total Internal Fragmentation: {_lastResult.InternalFragmentation} KB | External Fragmentation: {_lastResult.ExternalFragmentation} KB");
                }

                MessageBox.Show("Report exported successfully.", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"File export error:\n{ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private bool TryParseInputs(out List<MemoryBlock> blocks, out List<ProcessInfo> processes)
    {
        blocks = new();
        processes = new();

        var blockParts = TxtBlocks.Text.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
        int bIndex = 0;
        foreach (var bp in blockParts)
        {
            if (!int.TryParse(bp.Trim(), out int sz) || sz <= 0)
            {
                MessageBox.Show($"'{bp}' is not a valid block size.", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            blocks.Add(new MemoryBlock { Id = bIndex + 1, BlockIndex = bIndex, Size = sz });
            bIndex++;
        }

        var procParts = TxtProcesses.Text.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
        int pIndex = 0;
        foreach (var pp in procParts)
        {
            if (!int.TryParse(pp.Trim(), out int sz) || sz <= 0)
            {
                MessageBox.Show($"'{pp}' is not a valid process size.", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            processes.Add(new ProcessInfo { Id = pIndex + 1, Name = $"P{pIndex + 1}", Size = sz });
            pIndex++;
        }

        if (blocks.Count == 0 || processes.Count == 0)
        {
            MessageBox.Show("Memory blocks and processes cannot be empty.", "Input Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    public class AllocationViewModel
    {
        public string ProcessName { get; set; } = "";
        public string ProcessSize { get; set; } = "";
        public string BlockName { get; set; } = "";
        public string BlockSize { get; set; } = "";
        public string InternalFragText { get; set; } = "";
        public bool IsAllocated { get; set; }
        public string StatusText { get; set; } = "";
    }
}
