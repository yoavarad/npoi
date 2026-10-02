# Profiling the xls read path (Windows)

How to find CPU and allocation hotspots in the `byte[]` -> `HSSFWorkbook` read path. The results of the last run are in [benchmarks/read-path-hotspots.md](benchmarks/read-path-hotspots.md).

## Prerequisites

```powershell
dotnet tool install -g dotnet-trace
dotnet tool install -g dotnet-counters
```

Always profile a Release build (`-c Release`); Debug timings are meaningless.

## Workload: the console harness

`benchmarks/NPOI.ReadHarness` opens an xls from a `byte[]` N times and walks every cell. With no path it generates the same 60,000 x 8, 16.3 MB workbook as `ReadFromBytesBenchmark` (`large`) into `%TEMP%\npoi-large.xls` and reuses it.

```powershell
dotnet build benchmarks/NPOI.ReadHarness -c Release
dotnet benchmarks/NPOI.ReadHarness/bin/Release/net10.0/NPOI.ReadHarness.dll 8            # 8 iterations, generated fixture
dotnet benchmarks/NPOI.ReadHarness/bin/Release/net10.0/NPOI.ReadHarness.dll 8 C:\path\to\real.xls
```

It prints per-iteration cells, elapsed time and total allocated MB, then peak working set and GC counts. The first iteration includes JIT warm-up.

Do not attach `dotnet-trace` to a BenchmarkDotNet run: the benchmark runs in a child process and the trace captures only the launcher.

## 1. CPU: dotnet-trace sampling

```powershell
$h = "benchmarks/NPOI.ReadHarness/bin/Release/net10.0/NPOI.ReadHarness.dll"
dotnet-trace collect --profile dotnet-sampled-thread-time --output cpu.nettrace -- dotnet $h 8
dotnet-trace report cpu.nettrace topN -n 25
```

`topN` lists methods by exclusive percentage (and inclusive). Notes:

- `Thread.PollGCWorker` is time spent waiting in GC suspension; read it as "GC cost", not a method to fix.
- Reflection-created constructors (`RuntimeConstructorInfo.Invoke`) have a large inclusive share because the record parsing happens inside them. Use the exclusive column to judge the reflection overhead itself.
- To browse call trees, open the `.nettrace` in PerfView or Visual Studio, or convert with `dotnet-trace convert cpu.nettrace --format speedscope`.

## 2. Allocations: dotnet-trace gc-verbose

```powershell
dotnet-trace collect --profile gc-verbose --output gc.nettrace -- dotnet $h 3
```

`gc-verbose` records GC collections and `GCAllocationTick` events (one per ~100 KB allocated, tagged with type and call stack). The CLI has no allocation report, so aggregate them with TraceEvent. A throwaway console project (not committed) that groups ticks by type and by innermost NPOI frame:

```csharp
// <PackageReference Include="Microsoft.Diagnostics.Tracing.TraceEvent" Version="3.1.16" />
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

var etlx = new TraceLog(TraceLog.CreateFromEventPipeDataFile(args[0]));
var byType = new Dictionary<string, double>();
var bySite = new Dictionary<string, double>();
double total = 0;
foreach (var ev in etlx.Events)
{
    if (ev is not GCAllocationTickTraceData t) continue;
    double sz = t.AllocationAmount64;
    total += sz;
    byType[t.TypeName] = byType.GetValueOrDefault(t.TypeName) + sz;
    string site = "?";
    for (var f = ev.CallStack(); f != null; f = f.Caller)
    {
        var m = f.CodeAddress.FullMethodName;
        if (m.Contains("NPOI.")) { site = m; break; }
    }
    bySite[site] = bySite.GetValueOrDefault(site) + sz;
}
foreach (var kv in byType.OrderByDescending(k => k.Value).Take(10))
    Console.WriteLine($"{kv.Value / total * 100,5:F1}% {kv.Value / 1048576,8:F1} MB  {kv.Key}");
foreach (var kv in bySite.OrderByDescending(k => k.Value).Take(10))
    Console.WriteLine($"{kv.Value / total * 100,5:F1}% {kv.Value / 1048576,8:F1} MB  {kv.Key}");
```

Tick sampling makes the numbers statistical; the percentages are reliable, single small frames are not. PerfView's "GC Heap Alloc Stacks" view on the same `.nettrace` gives the full call tree if you need it.

## 3. CPU %, GC heap, allocation rate, working set: dotnet-counters

Live view:

```powershell
dotnet-counters monitor --counters System.Runtime -- dotnet $h 8
```

Recorded to CSV (easier to summarize):

```powershell
dotnet-counters collect --refresh-interval 1 --format csv --output counters.csv --counters System.Runtime -- dotnet $h 8
```

Counters of interest on .NET 10 (names differ from older runtimes):

| Need | Counter |
|---|---|
| CPU | `dotnet.process.cpu.time` (`cpu.mode=user` / `system`, seconds per second) |
| GC heap size | `dotnet.gc.last_collection.heap.size` per generation (`gen0`, `gen1`, `gen2`, `loh`, `poh`) |
| Allocation rate | `dotnet.gc.heap.total_allocated` (bytes per second) |
| Working set | `dotnet.process.memory.working_set` |
| Committed | `dotnet.gc.last_collection.memory.committed_size` |
| GC cost | `dotnet.gc.pause.time`, `dotnet.gc.collections` per generation |

Summarize the maximum of each counter:

```powershell
Import-Csv counters.csv | Group-Object 'Counter Name' | ForEach-Object {
  $m = $_.Group | ForEach-Object { [double]$_.'Mean/Increment' } | Measure-Object -Maximum
  "{0}: max {1:N0}" -f $_.Name, $m.Maximum }
```

## 4. BenchmarkDotNet EventPipeProfiler (not usable yet)

`EventPipeProfiler` (`--profiler EP`, or `[EventPipeProfiler(EventPipeProfile.CpuSampling)]` on a benchmark class) would write a `.nettrace` per benchmark into `BenchmarkDotNet.Artifacts`, to be analysed as in sections 1 and 2.

Tried on 2026-09-30 and it does not work with the pinned BenchmarkDotNet 0.13.12 on net10.0:

- `--profiler EP` fails validation with "EventPipeProfiler supports only .NET Core 3.0+": 0.13.12 does not recognise net10.0 as a .NET Core runtime (the same reason `ThreadingDiagnoser` is rejected, see the baseline doc).
- `--runtimes net10.0` is rejected as an invalid runtime, and `--runtimes hostprocess` throws while parsing the config.

Revisit after a BenchmarkDotNet upgrade to a version that recognises net10.0. Until then use the harness (sections 1 to 3); it also gives a long steady-state run, which short BenchmarkDotNet jobs do not.

## Reading the results

- Compare runs on the same machine only. Note the SDK version, fixture size and iteration count.
- Allocated bytes per op (`MemoryDiagnoser`, see [benchmarks/read-path-baseline.md](benchmarks/read-path-baseline.md)) is the stable metric for before/after claims; sampled CPU percentages shift with GC timing.
