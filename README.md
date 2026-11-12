# RAMbo – Memory Management System

<img width="2752" height="1536" alt="Page Replacement" src="https://github.com/user-attachments/assets/96a14368-c7f4-4649-8602-d4e76730edcf" />
<img width="2752" height="1536" alt="Memory Partitioning" src="https://github.com/user-attachments/assets/395f81a7-6c75-4d48-be0e-546ffa92e096" />
<img width="2752" height="1536" alt="Simulation History" src="https://github.com/user-attachments/assets/f92ccb08-ba59-4984-bdfe-3f25badf43b9" />

# RAMbo – Memory Management System

**RAMbo is a learning and analysis tool that shows how an operating system manages RAM.**

Memory management normally happens invisibly inside the OS kernel, which makes it hard to understand, test or compare. RAMbo makes it visible: you give it a workload, pick an algorithm, and it shows exactly what the operating system would do, step by step, and how well it performed.

---

## What Problem Does It Solve?

An operating system has two difficult memory questions to answer all the time:

1. **When RAM is full and a new page is needed, which page should be thrown out?**  
   A poor choice means the system keeps reloading pages from disk and slows down. This is the *page replacement* problem.

2. **When a process needs memory, which free region should it be given?**  
   A poor choice wastes memory and leaves many small, unusable gaps. This is the *memory partitioning* problem.

There is no single best answer; every algorithm trades simplicity against efficiency. RAMbo lets you run the classic algorithms on the same input and compare the results directly.

---

## What Can You Do With It?

### Simulate page replacement
Provide a sequence of page requests and a number of memory frames, or pick a built-in preset scenario. RAMbo replays the sequence across all algorithms at once and reports, for every step, what is in memory, whether it was a **HIT** or a **FAULT**, and how each algorithm compares.

| Algorithm | Idea |
|-----------|------|
| **FIFO** | Evict the page that arrived first |
| **LRU** | Evict the page that has not been used for the longest time |
| **Optimal** | Evict the page that will not be needed for the longest time (theoretical best, used as a benchmark) |

You get page fault count, hit count, hit ratio, a benchmark summary against Optimal, and a step-by-step comparative trace.

### Simulate memory partitioning
Provide a list of memory blocks and a list of processes with their sizes, or load a preset scenario. RAMbo places the processes using:

| Algorithm | Idea |
|-----------|------|
| **First Fit** | Take the first block that is big enough |
| **Best Fit** | Take the smallest block that is big enough |
| **Worst Fit** | Take the largest block that is big enough |

It supports both **fixed** partitioning (predefined blocks, one process per block) and **variable** partitioning (a block is split and used only as much as needed), and tracks memory efficiency:

- **Internal fragmentation:** space wasted *inside* an allocated block
- **External fragmentation:** free space that exists but cannot be used because no single block fits a waiting process
- **Unallocated processes:** count of processes that could not be placed in memory

### Save, export, and review simulation history
- **Database persistence:** Simulation results are stored in a Microsoft SQL Server database (`Save to DB`), with a live connection status indicator in the header.
- **History inspection:** Expand any saved run in the **Simulation History** tab to inspect detailed execution steps and memory allocations, refresh records, or delete old runs.
- **Export:** Export simulation results directly from the workspace for reports and offline analysis.

---

## Who Is It For?

- **Students** learning operating systems who want to see the textbook algorithms run on real input instead of working them out by hand
- **Instructors** who need a quick way to demonstrate the difference between algorithms
- **Anyone** who wants to experiment with how workload and memory size affect performance

---

## Quick Examples

### 1. Page Replacement
Reference string `7, 0, 1, 2, 0, 3, 0, 4, 2, 3, 0, 3, 2` with `3` frames:

| Algorithm | Page Faults | Hits | Hit Ratio |
|-----------|:-----------:|:----:|:---------:|
| FIFO | 10 | 3 | 23.1% |
| LRU | 9 | 4 | 30.8% |
| Optimal | 7 | 6 | 46.2% |

Optimal never does worse than the others. The gap between it and FIFO or LRU shows how much a real algorithm loses by not knowing the future.

### 2. Memory Partitioning (Fixed – First Fit)
Memory blocks `50, 100, 150, 200` KB with processes `45, 80, 120, 30` KB:

| Process | Process Size | Target Block | Block Size | Internal Fragmentation | Status |
|:-------:|:------------:|:------------:|:----------:|:----------------------:|:------:|
| P1 | 45 KB | Block 0 | 50 KB | 5 KB | ALLOCATED |
| P2 | 80 KB | Block 1 | 100 KB | 20 KB | ALLOCATED |
| P3 | 120 KB | Block 2 | 150 KB | 30 KB | ALLOCATED |
| P4 | 30 KB | Block 3 | 200 KB | 170 KB | ALLOCATED |

Total **Internal Fragmentation:** `225 KB` | **External Fragmentation:** `0 KB` | **Unallocated Processes:** `0`

---

## Running It

**Requirements:** Windows 10/11, .NET SDK 8.0 or later, Microsoft SQL Server (LocalDB works), Git.

```bash
git clone [https://github.com/htergun/RAMbo-MemoryManagementSystem.git](https://github.com/htergun/RAMbo-MemoryManagementSystem.git)
cd RAMbo-MemoryManagementSystem
```

1. Run `Database/01_schema.sql` on your SQL Server instance to create the database:
   ```bash
   sqlcmd -S "(localdb)\MSSQLLocalDB" -i "Database/01_schema.sql"
   ```
2. Set the `RamboDb` connection string in `src/RAMbo.App/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "RamboDb": "Server=(localdb)\\MSSQLLocalDB;Database=RamboMemoryDb;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```
3. Start the application (or open `RAMbo.sln` in Visual Studio and press `F5`):
   ```bash
   dotnet run --project src/RAMbo.App
   ```

---

## Built With & Architecture

- **Language & Runtime:** C# and .NET 8
- **Desktop UI:** Windows Presentation Foundation (WPF)
- **Data Access & Database:** Entity Framework Core, Microsoft SQL Server

```text
RAMbo-MemoryManagementSystem/
├── Database/
│   └── 01_schema.sql
├── Docs/
├── src/
│   └── RAMbo.App/
│       ├── Data/
│       ├── Models/
│       ├── Services/
│       ├── Views/
│       └── appsettings.json
└── RAMbo.sln
```

The code is organized in layers (`Data`, `Services`, `Models`, `Views`). Every algorithm implements a small interface, so new algorithms can be added without touching the rest of the application.

---

## Core Modules and Task Distribution

| Week | Topic | Tasks | Responsible Student |
| :---: | :--- | :--- | :--- |
| 1 | Research & Planning | Review concepts, define project scope & requirements. | All together |
| 2 | Architecture & DB Design | Design layered architecture, SQL schema & ER diagram. | Haydar Talha Ergün |
| 3 | Project Skeleton (.NET) | Setup .NET project structure, DB context & SQL connection. | Haydar Talha Ergün |
| 4 | FIFO Algorithm | Implement FIFO algorithm in C# & validate with SQL. | Özgürcan Akgül |
| 5 | LRU Algorithm | Implement LRU algorithm & compare with FIFO using test data. | Mustafa Devran Karaca |
| 6 | Optimal Algorithm | Implement Optimal algorithm & build comparison report. | Taha Akıncan |
| 7 | Memory Partitioning | Implement fixed/variable partitioning & fragmentation calc. | Haydar Talha Ergün |
| 8 | SQL Integration | Complete CRUD operations & run SQL queries. | Özgürcan Akgül |
| 9 | UI Development | Design input screens & results display (tables/charts). | Taha Akıncan |
| 10 | Testing & Debugging | Test edge cases, fix bugs & refactor code. | Mustafa Devran Karaca |
| 11 | Docs & Presentation | Prepare user report, presentation & final delivery check. | Haydar Talha Ergün |

---

## Team

**Group RAMbo**

| Name | Student No. |
|------|-------------|
| Haydar Talha Ergün | 202303001099 |
| Özgürcan Akgül | 202303001049 |
| Mustafa Devran Karaca | 202303001018 |
| Taha Akıncan | 202403001009 |

*Developed for COME 361 – Operating Systems.*
