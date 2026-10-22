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
Provide a sequence of page requests and a number of memory frames. RAMbo replays the sequence and reports, for every request, what is in memory, whether it was a **hit** or a **page fault**, and which page was evicted.

| Algorithm | Idea |
|-----------|------|
| **FIFO** | Evict the page that arrived first |
| **LRU** | Evict the page that has not been used for the longest time |
| **Optimal** | Evict the page that will not be needed for the longest time (theoretical best, used as a benchmark) |

You get page fault count, hit count and hit ratio, and a comparison table of all three.

### Simulate memory allocation
Provide a list of memory blocks and a list of processes with their sizes. RAMbo places the processes using:

| Algorithm | Idea |
|-----------|------|
| **First Fit** | Take the first block that is big enough |
| **Best Fit** | Take the smallest block that is big enough |
| **Worst Fit** | Take the largest block that is big enough |

It supports both **fixed** partitioning (predefined blocks, one process per block) and **variable** partitioning (a block is used only as much as needed), and calculates how much memory is wasted:

- **Internal fragmentation:** space wasted *inside* an allocated block
- **External fragmentation:** free space that exists but cannot be used because no process fits

### Keep a history of runs
Simulation results are stored in a SQL Server database, so runs can be saved, reviewed and compared later.

---

## Who Is It For?

- **Students** learning operating systems who want to see the textbook algorithms run on real input instead of working them out by hand
- **Instructors** who need a quick way to demonstrate the difference between algorithms
- **Anyone** who wants to experiment with how workload and memory size affect performance

---

## A Quick Example

Reference string `7, 0, 1, 2, 0, 3, 0, 4, 2, 3, 0, 3, 2` with 3 frames:

| Algorithm | Page Faults | Hits |
|-----------|:-----------:|:----:|
| FIFO | 10 | 3 |
| LRU | 9 | 4 |
| Optimal | 7 | 6 |

Optimal never does worse than the others. The gap between it and FIFO or LRU shows how much a real algorithm loses by not knowing the future.

---

## Running It

**Requirements:** .NET SDK 8.0 or later, Microsoft SQL Server (LocalDB works), Git.

```bash
git clone https://github.com/htergun/RAMbo-MemoryManagementSystem.git
cd RAMbo-MemoryManagementSystem
```

1. Run `Database/01_schema.sql` on your SQL Server instance to create the database.
2. Set the `RamboDb` connection string in `src/RAMbo.App/appsettings.json`.
3. Start the application:

```bash
dotnet run --project src/RAMbo.App
```

---

## Built With

C# and .NET 8, Entity Framework Core, Microsoft SQL Server.

The code is organized in layers (data access, services, models). Every algorithm implements a small interface, so new algorithms can be added without touching the rest of the application.

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
