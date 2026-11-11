using RAMbo.App.Data;
using RAMbo.App.Models;
using RAMbo.App.Services.Interfaces;

namespace RAMbo.App.Services;

/// <summary>Algoritmayı çalıştırır ve sonucu veritabanına kaydeder. UI bu sınıfı kullanır.</summary>
public class SimulationRunner
{
    private readonly SimulationRepository _repo;

    public SimulationRunner(SimulationRepository repo) => _repo = repo;

    public SimulationResult RunPageReplacement(IPageReplacementAlgorithm algo, int[] refs, int frames, bool save = true)
    {
        var result = algo.Run(refs, frames);
        if (save) _repo.SavePageReplacement(result);
        return result;
    }

    public PartitionResult RunPartitioning(IPartitionAlgorithm algo, List<MemoryBlock> blocks, List<ProcessInfo> processes)
    {
        return algo.Run(blocks, processes);
    }

    public PartitionResult RunPartitioningAndSave(
    IPartitionAlgorithm algo,
    List<MemoryBlock> blocks,
    List<ProcessInfo> processes,
    string mode)
    {
        var result = RunPartitioning(algo, blocks, processes);

        _repo.SavePartitioning(result, blocks, processes, mode);

        return result;
    }
}