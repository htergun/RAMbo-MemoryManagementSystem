using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RAMbo.App.Data;
using RAMbo.App.Models;
using RAMbo.App.Services;
using RAMbo.App.Services.Algorithms;
using RAMbo.App.Services.Interfaces;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var options = new DbContextOptionsBuilder<RamboDbContext>()
    .UseSqlServer(config.GetConnectionString("RamboDb"))
    .Options;

using var db = new RamboDbContext(options);
var repo = new SimulationRepository(db);
var runner = new SimulationRunner(repo);

Console.WriteLine("RAMbo - Bellek Yonetimi Simulatoru");
Console.WriteLine(new string('=', 50));

// Baglanti testi
try
{
    Console.WriteLine(db.Database.CanConnect()
        ? "Veritabani baglantisi: OK"
        : "Veritabani baglantisi BASARISIZ. Database/01_schema.sql dosyasini calistirdiniz mi?");
}
catch (Exception ex)
{
    Console.WriteLine("Baglanti hatasi: " + ex.Message);
}

// -------------------------------------------------------
// Hafta 6: Optimal Algoritmasi - Test & Karsilastirma
// -------------------------------------------------------
var refs = new[] { 7, 0, 1, 2, 0, 3, 0, 4, 2, 3, 0, 3, 2 };
int frameCount = 3;

Console.WriteLine($"\nReferans Dizisi: [{string.Join(", ", refs)}]");
Console.WriteLine($"Frame Sayisi   : {frameCount}\n");

// Tek algoritma detayli cikti (Optimal)
var optResult = runner.RunPageReplacement(new OptimalAlgorithm(), refs, frameCount, save: false);
Console.WriteLine($"--- {optResult.AlgorithmName} Adim Adim ---");
Console.WriteLine($"{"Adim",4} | {"Sayfa",5} | {"Frames",-14} | {"Fault?",6} | {"Victim",6}");
Console.WriteLine(new string('-', 50));
foreach (var step in optResult.Steps)
{
    Console.WriteLine($"{step.StepNumber,4} | {step.PageNumber,5} | {step.FrameState,-14} | {(step.IsFault ? "EVET  " : "HAYIR "),6} | {step.VictimPage?.ToString() ?? "-",6}");
}
Console.WriteLine($"\nPage Fault: {optResult.PageFaults} | Hit: {optResult.Hits} | Hit Ratio: {optResult.HitRatio}%");

// -------------------------------------------------------
// Hafta 4: FIFO Algoritmasi - Test
// -------------------------------------------------------
var fifoResult = runner.RunPageReplacement(new FifoAlgorithm(), refs, frameCount, save: false);
Console.WriteLine($"\n--- {fifoResult.AlgorithmName} Adim Adim ---");
Console.WriteLine($"{"Adim",4} | {"Sayfa",5} | {"Frames",-14} | {"Fault?",6} | {"Victim",6}");
Console.WriteLine(new string('-', 50));
foreach (var step in fifoResult.Steps)
{
    Console.WriteLine($"{step.StepNumber,4} | {step.PageNumber,5} | {step.FrameState,-14} | {(step.IsFault ? "EVET  " : "HAYIR "),6} | {step.VictimPage?.ToString() ?? "-",6}");
}
Console.WriteLine($"\nPage Fault: {fifoResult.PageFaults} | Hit: {fifoResult.Hits} | Hit Ratio: {fifoResult.HitRatio}%");

// Belady anomalisi testi: frame artinca FIFO'da fault artar (3 frame -> 9, 4 frame -> 10)
Console.WriteLine("\n--- FIFO Belady Anomalisi Testi ---");
var beladyRefs = new[] { 1, 2, 3, 4, 1, 2, 5, 1, 2, 3, 4, 5 };
foreach (var fc in new[] { 3, 4 })
{
    var bel = runner.RunPageReplacement(new FifoAlgorithm(), beladyRefs, fc, save: false);
    Console.WriteLine($"{fc} frame: {bel.PageFaults} fault");
}

// Karsilastirma tablosu (LRU hazir oldugunda aktif edilecek)
IPageReplacementAlgorithm[] algorithms = [new FifoAlgorithm(), new LruAlgorithm(), new OptimalAlgorithm()];
Console.WriteLine("\n=== KARSILASTIRMA RAPORU ===");
Console.WriteLine($"{"Algoritma",-12} | {"Page Fault",10} | {"Hit",6} | {"Hit Ratio",10}");
Console.WriteLine(new string('-', 48));
foreach (var algo in algorithms)
{
    var r = runner.RunPageReplacement(algo, refs, frameCount, save: false);
    Console.WriteLine($"{r.AlgorithmName,-12} | {r.PageFaults,10} | {r.Hits,6} | {r.HitRatio,9}%");
}

// -------------------------------------------------------
// Hafta 7: Memory Partitioning - Test & Karsilastirma
// -------------------------------------------------------
var blocks = new List<MemoryBlock>
{
    new() { Id = 1, BlockIndex = 0, Size = 100 },
    new() { Id = 2, BlockIndex = 1, Size = 500 },
    new() { Id = 3, BlockIndex = 2, Size = 200 },
    new() { Id = 4, BlockIndex = 3, Size = 300 },
    new() { Id = 5, BlockIndex = 4, Size = 600 },
};
var procs = new List<ProcessInfo>
{
    new() { Id = 1, Name = "P1", Size = 212 },
    new() { Id = 2, Name = "P2", Size = 417 },
    new() { Id = 3, Name = "P3", Size = 112 },
    new() { Id = 4, Name = "P4", Size = 426 },
};

Console.WriteLine("\n=== MEMORY PARTITIONING RAPORU ===");
Console.WriteLine($"Bloklar   : [{string.Join(", ", blocks.Select(b => b.Size))}]");
Console.WriteLine($"Processler: [{string.Join(", ", procs.Select(p => p.Size))}]\n");
Console.WriteLine($"{"Mod",-9} | {"Algoritma",-10} | {"Internal",8} | {"External",8} | {"Yerlesemeyen",12}");
Console.WriteLine(new string('-', 60));
foreach (var mode in new[] { PartitionMode.Fixed, PartitionMode.Variable })
{
    PartitionAlgorithmBase[] partAlgos =
    {
        new FirstFit { Mode = mode },
        new BestFit { Mode = mode },
        new WorstFit { Mode = mode }
    };

    foreach (var pAlgo in partAlgos)
    {
        var pr = runner.RunPartitioningAndSave(pAlgo, blocks, procs, mode.ToString());
        int failed = pr.Allocations.Count(a => a.MemoryBlockId == null);
        Console.WriteLine($"{mode,-9} | {pr.AlgorithmName,-10} | {pr.InternalFragmentation,8} | {pr.ExternalFragmentation,8} | {failed,12}");
    }
}

Console.ReadLine();