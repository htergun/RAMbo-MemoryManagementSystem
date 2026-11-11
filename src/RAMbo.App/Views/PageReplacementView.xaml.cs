using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using RAMbo.App.Models;
using RAMbo.App.Services;
using RAMbo.App.Services.Algorithms;
using RAMbo.App.Services.Interfaces;

namespace RAMbo.App.Views;

public partial class PageReplacementView : UserControl
{
    private SimulationRunner? _runner;
    private SimulationResult? _fifoResult;
    private SimulationResult? _lruResult;
    private SimulationResult? _optimalResult;

    private static readonly (string Name, string Refs, int Frames)[] Presets =
    {
        ("— Select Preset Scenario —", "", 3),
        ("Standard Scenario (13 refs, 3 frames)", "7, 0, 1, 2, 0, 3, 0, 4, 2, 3, 0, 3, 2", 3),
        ("Belady's Anomaly (12 refs, 3 frames - FIFO: 9 Faults)", "1, 2, 3, 4, 1, 2, 5, 1, 2, 3, 4, 5", 3),
        ("Belady's Anomaly (12 refs, 4 frames - FIFO: 10 Faults [Edge Case])", "1, 2, 3, 4, 1, 2, 5, 1, 2, 3, 4, 5", 4),
        ("Basic Sequence (7 refs, 3 frames)", "1, 2, 3, 4, 1, 2, 5", 3)
    };

    public PageReplacementView()
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
        TxtReferences.Text = preset.Refs;
        TxtFrameCount.Text = preset.Frames.ToString();
    }

    private void BtnRunAll_Click(object sender, RoutedEventArgs e)
    {
        if (_runner == null) return;

        if (!TryParseInputs(out int[] refs, out int frameCount))
            return;

        try
        {
            // Execute all three algorithms concurrently on the same inputs
            _fifoResult = _runner.RunPageReplacement(new FifoAlgorithm(), refs, frameCount, save: false);
            _lruResult = _runner.RunPageReplacement(new LruAlgorithm(), refs, frameCount, save: false);
            _optimalResult = _runner.RunPageReplacement(new OptimalAlgorithm(), refs, frameCount, save: false);

            DisplayComparativeResults(_fifoResult, _lruResult, _optimalResult, refs);

            BtnSaveAll.IsEnabled = true;
            BtnExport.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to execute algorithms:\n{ex.Message}", "Execution Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DisplayComparativeResults(SimulationResult fifo, SimulationResult lru, SimulationResult opt, int[] refs)
    {
        int minFaults = Math.Min(fifo.PageFaults, Math.Min(lru.PageFaults, opt.PageFaults));

        // 1. Benchmark Summary
        var summaryList = new List<AlgorithmSummaryItem>
        {
            new()
            {
                AlgorithmName = "FIFO (First-In, First-Out)",
                PageFaults = fifo.PageFaults,
                Hits = fifo.Hits,
                HitRatioText = $"{fifo.HitRatio:F1}%",
                IsBest = fifo.PageFaults == minFaults,
                BenchmarkNote = fifo.PageFaults == minFaults ? "Lowest Fault Count" : $"+{fifo.PageFaults - minFaults} Faults vs Best"
            },
            new()
            {
                AlgorithmName = "LRU (Least Recently Used)",
                PageFaults = lru.PageFaults,
                Hits = lru.Hits,
                HitRatioText = $"{lru.HitRatio:F1}%",
                IsBest = lru.PageFaults == minFaults,
                BenchmarkNote = lru.PageFaults == minFaults ? "Lowest Fault Count" : $"+{lru.PageFaults - minFaults} Faults vs Best"
            },
            new()
            {
                AlgorithmName = "Optimal (Theoretical Limit)",
                PageFaults = opt.PageFaults,
                Hits = opt.Hits,
                HitRatioText = $"{opt.HitRatio:F1}%",
                IsBest = opt.PageFaults == minFaults,
                BenchmarkNote = "Theoretical Optimal Benchmark"
            }
        };

        GridSummary.ItemsSource = summaryList;

        // 2. Step-by-Step Comparative Trace
        var stepCount = refs.Length;
        var comparisonSteps = new List<ComparativeStepItem>();

        var faultBg = new SolidColorBrush(Color.FromRgb(254, 226, 226));
        var faultFg = new SolidColorBrush(Color.FromRgb(220, 38, 38));
        var hitBg = new SolidColorBrush(Color.FromRgb(220, 252, 231));
        var hitFg = new SolidColorBrush(Color.FromRgb(22, 163, 74));

        for (int i = 0; i < stepCount; i++)
        {
            var fStep = fifo.Steps.ElementAtOrDefault(i);
            var lStep = lru.Steps.ElementAtOrDefault(i);
            var oStep = opt.Steps.ElementAtOrDefault(i);

            comparisonSteps.Add(new ComparativeStepItem
            {
                StepNumber = i + 1,
                PageNumber = refs[i],

                FifoFrames = fStep != null ? $"[ {fStep.FrameState} ]" : "-",
                FifoStatus = fStep != null && fStep.IsFault ? "FAULT" : "HIT",
                FifoStatusBg = fStep != null && fStep.IsFault ? faultBg : hitBg,
                FifoStatusFg = fStep != null && fStep.IsFault ? faultFg : hitFg,

                LruFrames = lStep != null ? $"[ {lStep.FrameState} ]" : "-",
                LruStatus = lStep != null && lStep.IsFault ? "FAULT" : "HIT",
                LruStatusBg = lStep != null && lStep.IsFault ? faultBg : hitBg,
                LruStatusFg = lStep != null && lStep.IsFault ? faultFg : hitFg,

                OptimalFrames = oStep != null ? $"[ {oStep.FrameState} ]" : "-",
                OptimalStatus = oStep != null && oStep.IsFault ? "FAULT" : "HIT",
                OptimalStatusBg = oStep != null && oStep.IsFault ? faultBg : hitBg,
                OptimalStatusFg = oStep != null && oStep.IsFault ? faultFg : hitFg
            });
        }

        GridSteps.ItemsSource = comparisonSteps;
        ResultsContainer.Visibility = Visibility.Visible;
    }

    private void BtnSaveAll_Click(object sender, RoutedEventArgs e)
    {
        if (_runner == null || _fifoResult == null || _lruResult == null || _optimalResult == null) return;

        try
        {
            _runner.RunPageReplacement(new FifoAlgorithm(), _fifoResult.ReferenceString, _fifoResult.FrameCount, save: true);
            _runner.RunPageReplacement(new LruAlgorithm(), _lruResult.ReferenceString, _lruResult.FrameCount, save: true);
            _runner.RunPageReplacement(new OptimalAlgorithm(), _optimalResult.ReferenceString, _optimalResult.FrameCount, save: true);

            MessageBox.Show("All 3 algorithm simulations (FIFO, LRU, Optimal) successfully saved to database.", "Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save records:\n{ex.Message}", "Database Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        if (_fifoResult == null || _lruResult == null || _optimalResult == null) return;

        var dlg = new SaveFileDialog
        {
            Filter = "Text File (*.txt)|*.txt|CSV File (*.csv)|*.csv",
            FileName = $"PageReplacement_Comparison_{DateTime.Now:yyyyMMdd_HHmm}"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                using var sw = new StreamWriter(dlg.FileName);
                bool isCsv = dlg.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

                if (isCsv)
                {
                    sw.WriteLine("Step,PageRequested,FIFO_Frames,FIFO_Status,LRU_Frames,LRU_Status,Optimal_Frames,Optimal_Status");
                    for (int i = 0; i < _fifoResult.ReferenceString.Length; i++)
                    {
                        var f = _fifoResult.Steps.ElementAtOrDefault(i);
                        var l = _lruResult.Steps.ElementAtOrDefault(i);
                        var o = _optimalResult.Steps.ElementAtOrDefault(i);

                        sw.WriteLine($"{i + 1},{_fifoResult.ReferenceString[i]},\"{f?.FrameState}\",{(f != null && f.IsFault ? "FAULT" : "HIT")},\"{l?.FrameState}\",{(l != null && l.IsFault ? "FAULT" : "HIT")},\"{o?.FrameState}\",{(o != null && o.IsFault ? "FAULT" : "HIT")}");
                    }
                    sw.WriteLine();
                    sw.WriteLine("Algorithm,TotalPageFaults,Hits,HitRatio");
                    sw.WriteLine($"FIFO,{_fifoResult.PageFaults},{_fifoResult.Hits},{_fifoResult.HitRatio}%");
                    sw.WriteLine($"LRU,{_lruResult.PageFaults},{_lruResult.Hits},{_lruResult.HitRatio}%");
                    sw.WriteLine($"Optimal,{_optimalResult.PageFaults},{_optimalResult.Hits},{_optimalResult.HitRatio}%");
                }
                else
                {
                    sw.WriteLine("==========================================================================");
                    sw.WriteLine("RAMbo - Simultaneous Page Replacement Comparison Benchmark");
                    sw.WriteLine($"Frame Count : {_fifoResult.FrameCount}");
                    sw.WriteLine($"References  : [{string.Join(", ", _fifoResult.ReferenceString)}]");
                    sw.WriteLine("==========================================================================");
                    sw.WriteLine(string.Format("{0,-15} | {1,-12} | {2,-8} | {3,-12}", "Algorithm", "Page Faults", "Hits", "Hit Ratio"));
                    sw.WriteLine(new string('-', 55));
                    sw.WriteLine(string.Format("{0,-15} | {1,-12} | {2,-8} | {3,-12}", "FIFO", _fifoResult.PageFaults, _fifoResult.Hits, $"{_fifoResult.HitRatio}%"));
                    sw.WriteLine(string.Format("{0,-15} | {1,-12} | {2,-8} | {3,-12}", "LRU", _lruResult.PageFaults, _lruResult.Hits, $"{_lruResult.HitRatio}%"));
                    sw.WriteLine(string.Format("{0,-15} | {1,-12} | {2,-8} | {3,-12}", "Optimal", _optimalResult.PageFaults, _optimalResult.Hits, $"{_optimalResult.HitRatio}%"));
                    sw.WriteLine(new string('=', 74));
                    sw.WriteLine(string.Format("{0,-5} | {1,-5} | {2,-16} | {3,-7} | {4,-16} | {5,-7} | {6,-16} | {7,-7}", 
                        "Step", "Page", "FIFO Frames", "FIFO", "LRU Frames", "LRU", "Optimal Frames", "Opt"));
                    sw.WriteLine(new string('-', 85));
                    for (int i = 0; i < _fifoResult.ReferenceString.Length; i++)
                    {
                        var f = _fifoResult.Steps.ElementAtOrDefault(i);
                        var l = _lruResult.Steps.ElementAtOrDefault(i);
                        var o = _optimalResult.Steps.ElementAtOrDefault(i);

                        sw.WriteLine(string.Format("{0,-5} | {1,-5} | {2,-16} | {3,-7} | {4,-16} | {5,-7} | {6,-16} | {7,-7}",
                            i + 1,
                            _fifoResult.ReferenceString[i],
                            $"[{f?.FrameState}]", f != null && f.IsFault ? "FAULT" : "HIT",
                            $"[{l?.FrameState}]", l != null && l.IsFault ? "FAULT" : "HIT",
                            $"[{o?.FrameState}]", o != null && o.IsFault ? "FAULT" : "HIT"));
                    }
                }

                MessageBox.Show("Comparative benchmark report exported successfully.", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"File export error:\n{ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private bool TryParseInputs(out int[] refs, out int frameCount)
    {
        refs = Array.Empty<int>();
        frameCount = 0;

        if (string.IsNullOrWhiteSpace(TxtReferences.Text))
        {
            MessageBox.Show("Please provide a valid page reference string.", "Input Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var parts = TxtReferences.Text.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var refList = new List<int>();
        foreach (var p in parts)
        {
            if (!int.TryParse(p.Trim(), out int val))
            {
                MessageBox.Show($"'{p}' is not a valid integer page number.", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            refList.Add(val);
        }

        if (!int.TryParse(TxtFrameCount.Text.Trim(), out frameCount) || frameCount <= 0 || frameCount > 20)
        {
            MessageBox.Show("Frame count must be an integer between 1 and 20.", "Invalid Frame Count",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        refs = refList.ToArray();
        return true;
    }

    public class AlgorithmSummaryItem
    {
        public string AlgorithmName { get; set; } = "";
        public int PageFaults { get; set; }
        public int Hits { get; set; }
        public string HitRatioText { get; set; } = "";
        public bool IsBest { get; set; }
        public string BenchmarkNote { get; set; } = "";
    }

    public class ComparativeStepItem
    {
        public int StepNumber { get; set; }
        public int PageNumber { get; set; }

        public string FifoFrames { get; set; } = "";
        public string FifoStatus { get; set; } = "";
        public Brush FifoStatusBg { get; set; } = Brushes.Transparent;
        public Brush FifoStatusFg { get; set; } = Brushes.Black;

        public string LruFrames { get; set; } = "";
        public string LruStatus { get; set; } = "";
        public Brush LruStatusBg { get; set; } = Brushes.Transparent;
        public Brush LruStatusFg { get; set; } = Brushes.Black;

        public string OptimalFrames { get; set; } = "";
        public string OptimalStatus { get; set; } = "";
        public Brush OptimalStatusBg { get; set; } = Brushes.Transparent;
        public Brush OptimalStatusFg { get; set; } = Brushes.Black;
    }
}
