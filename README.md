# RAMbo – Memory Management System

**COME 361 – Operating Systems | Term Project (Chapter 9: Main Memory / Virtual Memory)**

RAMbo is a .NET console application that simulates two core operating-system memory-management mechanisms and stores the simulation results in a SQL Server database:

- **Page replacement:** FIFO, LRU, Optimal
- **Memory partitioning:** First Fit, Best Fit, Worst Fit (fixed and variable partitioning) with internal / external fragmentation calculation

---

## Team

| # | Name | Student No. |
|---|------|-------------|
| 1 | Haydar Talha Ergün | 202303001099 |
| 2 | Özgürcan Akgül | 202303001049 |
| 3 | Mustafa Devran Karaca | 202303001018 |
| 4 | Taha Akıncan | 202403001009 |

**Group name:** RAMbo  
**Repository:** https://github.com/htergun/RAMbo-MemoryManagementSystem

---

## Features

### Page replacement algorithms
Given a reference string and a frame count, each algorithm produces a step-by-step trace (page, frame state, fault / hit, victim page) and summary statistics (page faults, hits, hit ratio).

| Algorithm | Victim selection rule |
|-----------|-----------------------|
| FIFO | The page that has been in memory the longest |
| LRU | The page that has not been used for the longest time |
| Optimal | The page that will not be used for the longest time in the future |

### Memory partitioning algorithms
Given a list of memory blocks and a list of processes, each algorithm places the processes and reports fragmentation.

| Algorithm | Placement rule |
|-----------|----------------|
| First Fit | First block that is large enough |
| Best Fit | Smallest block that is large enough |
| Worst Fit | Largest block that is large enough |

Two modes are supported (`PartitionMode`):

- **Fixed:** a block holds at most one process. The unused part of the block is **internal fragmentation** (`block size − process size`).
- **Variable:** a block is used only as much as the process needs; the remaining space stays available for other processes (internal fragmentation is 0).

**External fragmentation** is the total free memory that remains unused while at least one process could not be placed.

---

## Architecture

The project uses a layered structure (UI / Business Logic / Data Access):

```
src/RAMbo.App/
├── Program.cs                       # Entry point, DB connection check, demo/test output
├── appsettings.json                 # Connection string ("RamboDb")
├── Data/
│   ├── RamboDbContext.cs            # Entity Framework Core DbContext
│   └── SimulationRepository.cs      # Saves / reads simulation results (SQL)
├── Models/
│   ├── Simulation.cs, SimulationResult.cs, StepResult.cs, PageReference.cs
│   └── MemoryBlock.cs, ProcessInfo.cs, Allocation.cs, PartitionResult.cs
└── Services/
    ├── SimulationRunner.cs          # Runs an algorithm and saves the result (used by the UI)
    ├── Interfaces/
    │   ├── IPageReplacementAlgorithm.cs
    │   └── IPartitionAlgorithm.cs
    └── Algorithms/
        ├── FifoAlgorithm.cs, LruAlgorithm.cs, OptimalAlgorithm.cs
        ├── PartitionAlgorithmBase.cs   # Shared placement + fragmentation logic
        ├── PartitionMode.cs            # Fixed / Variable
        └── FirstFit.cs, BestFit.cs, WorstFit.cs
```

Design notes:

- Every page replacement algorithm implements `IPageReplacementAlgorithm` (`Run(int[] referenceString, int frameCount)`).
- Every partitioning algorithm implements `IPartitionAlgorithm` (`Run(List<MemoryBlock>, List<ProcessInfo>)`). First / Best / Worst Fit inherit from `PartitionAlgorithmBase` and only define how a block is selected.
- `SimulationRunner` is the single entry point for the UI: `RunPageReplacement(...)` and `RunPartitioning(...)`.

Database design and ER diagram: see `Mimari_ve_Veritabani.md`. The schema script is `Database/01_schema.sql`.

---

## Getting Started

### Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) or later
- SQL Server (LocalDB, Express or full edition)
- Git
- Visual Studio 2022+ (optional, `dotnet` CLI is enough)

### 1. Clone the repository

```bash
git clone https://github.com/htergun/RAMbo-MemoryManagementSystem.git
cd RAMbo-MemoryManagementSystem
```

### 2. Create the database

Run `Database/01_schema.sql` against your SQL Server instance (SSMS, Azure Data Studio, or Visual Studio's *SQL Server Object Explorer*). It creates the `RamboDb` database and its tables.

### 3. Configure the connection string

Edit `src/RAMbo.App/appsettings.json` and set the `RamboDb` connection string for your machine, for example:

```json
{
  "ConnectionStrings": {
    "RamboDb": "Server=(localdb)\\MSSQLLocalDB;Database=RamboDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### 4. Build

```bash
dotnet build
```

### 5. Run

```bash
dotnet run --project src/RAMbo.App
```

Or open the solution in Visual Studio and press **Ctrl+F5** (Start Without Debugging) so the console window stays open.

> If the database is unreachable, the program prints a connection warning and still runs the algorithm demos, because they do not depend on the database.

---

## Demo / Example Output

The demo in `Program.cs` uses the classic reference string `7, 0, 1, 2, 0, 3, 0, 4, 2, 3, 0, 3, 2` with 3 frames:

| Algorithm | Page Faults | Hits |
|-----------|-------------|------|
| FIFO | 10 | 3 |
| LRU | 9 | 4 |
| Optimal | 7 | 6 |

Partitioning demo — blocks `[100, 500, 200, 300, 600]`, processes `[212, 417, 112, 426]`:

| Mode | Algorithm | Internal | External | Unplaced |
|------|-----------|----------|----------|----------|
| Fixed | First Fit | 559 | 400 | 1 |
| Fixed | Best Fit | 433 | 0 | 0 |
| Fixed | Worst Fit | 659 | 300 | 1 |
| Variable | First Fit | 0 | 959 | 1 |
| Variable | Best Fit | 0 | 0 | 0 |
| Variable | Worst Fit | 0 | 959 | 1 |

### Usage example (code)

```csharp
var runner = new SimulationRunner(repo);

// Page replacement
var refs = new[] { 7, 0, 1, 2, 0, 3, 0, 4, 2, 3, 0, 3, 2 };
var result = runner.RunPageReplacement(new LruAlgorithm(), refs, frames: 3, save: false);
Console.WriteLine($"{result.AlgorithmName}: {result.PageFaults} page faults");

// Memory partitioning
var partition = runner.RunPartitioning(
    new BestFit { Mode = PartitionMode.Fixed }, blocks, processes);
Console.WriteLine($"Internal: {partition.InternalFragmentation}, External: {partition.ExternalFragmentation}");
```

---

## Project Status

| Week | Module | Status |
|------|--------|--------|
| 1 | Research & planning | Done |
| 2 | Architecture & DB design | Done |
| 3 | Project skeleton (.NET, folders, SQL connection) | Done |
| 4 | FIFO algorithm | Done |
| 5 | LRU algorithm | Done |
| 6 | Optimal algorithm + comparison report | Done |
| 7 | Memory partitioning (First / Best / Worst Fit, fragmentation) | Done |
| 8 | SQL integration (CRUD, history queries) | Planned |
| 9 | UI development (input screens, results display) | Planned |
| 10 | Testing & debugging | Planned |
| 11 | Documentation & presentation | Planned |

---

## Task Distribution

| Module | Owner |
|--------|-------|
| FIFO algorithm | Özgürcan Akgül |
| LRU algorithm | Mustafa Devran Karaca |
| Optimal algorithm + comparison report | Taha Akıncan |
| Memory partitioning (First / Best / Worst Fit) | Haydar Talha Ergün |
| SQL integration (Week 8) | Not yet |
| UI (Week 9) | Not yet |

---

## Tech Stack

- **Language:** C# (.NET 8), console application
- **Database:** Microsoft SQL Server
- **Data access:** Entity Framework Core (SQL Server provider)
- **Configuration:** `Microsoft.Extensions.Configuration` (`appsettings.json`)
- **Version control:** Git / GitHub

---

## Contributing / Workflow

1. Run `git pull origin main` before you start working.
2. Work on your own module's files only (see *Task Distribution*).
3. Commit with a clear message, e.g. `Week 7: First/Best/Worst Fit and fragmentation`.
4. `git pull origin main`, then `git push origin main`.

---

## License

Academic project for COME 361 – Operating Systems. Not intended for production use.
