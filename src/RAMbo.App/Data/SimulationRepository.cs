using Microsoft.EntityFrameworkCore;
using RAMbo.App.Models;

namespace RAMbo.App.Data;

/// <summary>Hafta 8'de CRUD tamamlanacak. Şimdilik kaydet, listele, getir, sil var.</summary>
public class SimulationRepository
{
    private readonly RamboDbContext _db;

    public SimulationRepository(RamboDbContext db) => _db = db;

    public int SavePageReplacement(SimulationResult r)
    {
        var sim = new Simulation
        {
            Name = $"{r.AlgorithmName} - {r.FrameCount} frame",
            SimulationType = "PageReplacement",
            AlgorithmName = r.AlgorithmName,
            FrameCount = r.FrameCount,
            TotalPageFaults = r.PageFaults,
            TotalHits = r.Hits,
            HitRatio = r.HitRatio,
            PageReferences = r.ReferenceString
                .Select((p, i) => new PageReference { SequenceOrder = i + 1, PageNumber = p }).ToList(),
            Results = r.Steps
        };
        _db.Simulations.Add(sim);
        _db.SaveChanges();
        return sim.Id;
    }

    public List<Simulation> GetAll() =>
        _db.Simulations.AsNoTracking().OrderByDescending(s => s.CreatedAt).ToList();

    public Simulation? GetById(int id) =>
        _db.Simulations.Include(s => s.Results).Include(s => s.PageReferences)
            .FirstOrDefault(s => s.Id == id);

    public void Delete(int id)
    {
        var s = _db.Simulations.Find(id);
        if (s == null) return;
        _db.Simulations.Remove(s);
        _db.SaveChanges();
    }


    public bool Update(int id, string newName)
    {
        var simulation = _db.Simulations.Find(id);

        if (simulation == null)
        {
            return false;
        }

        simulation.Name = newName;
        _db.SaveChanges();

        return true;
    }

public int SavePartitioning(
    PartitionResult result,
    List<MemoryBlock> blocks,
    List<ProcessInfo> processes,
    string mode)
{
    if (result.Allocations.Count != processes.Count)
        throw new InvalidOperationException("Allocation count does not match process count.");

    if (blocks.Select(b => b.Id).Distinct().Count() != blocks.Count)
        throw new InvalidOperationException("Memory block IDs must be unique.");

    for (int i = 0; i < processes.Count; i++)
    {
        if (result.Allocations[i].ProcessId != processes[i].Id)
            throw new InvalidOperationException("Allocation process order does not match.");
    }

    using var transaction = _db.Database.BeginTransaction();

    var simulation = new Simulation
    {
        Name = $"{result.AlgorithmName} - {mode}",
        SimulationType = "Partitioning",
        AlgorithmName = result.AlgorithmName,
        InternalFragmentation = result.InternalFragmentation,
        ExternalFragmentation = result.ExternalFragmentation,
        Processes = processes.Select(p => new ProcessInfo
        {
            Name = p.Name,
            Size = p.Size
        }).ToList(),
        MemoryBlocks = blocks.Select(b => new MemoryBlock
        {
            BlockIndex = b.BlockIndex,
            Size = b.Size
        }).ToList()
    };

    _db.Simulations.Add(simulation);
    _db.SaveChanges();

    var blockMap = blocks
        .Select((b, i) => new { b.Id, Index = i })
        .ToDictionary(x => x.Id, x => simulation.MemoryBlocks[x.Index].Id);

    var processMap = processes
        .Select((p, i) => new { p.Id, Index = i })
        .ToDictionary(x => x.Id, x => simulation.Processes[x.Index].Id);

    for (int i = 0; i < result.Allocations.Count; i++)
    {
        var allocation = result.Allocations[i];

        _db.Allocations.Add(new Allocation
        {
            SimulationId = simulation.Id,
            ProcessId = processMap[allocation.ProcessId],
            MemoryBlockId = allocation.MemoryBlockId.HasValue
                ? blockMap[allocation.MemoryBlockId.Value]
                : null,
            InternalFragmentation = allocation.InternalFragmentation
        });
    }

    _db.SaveChanges();
    transaction.Commit();

    return simulation.Id;
}

}
