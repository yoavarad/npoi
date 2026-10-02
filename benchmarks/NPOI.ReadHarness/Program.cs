using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.Diagnostics;

// Profiling harness for the xls read path (see docs/profiling.md).
// Usage: NPOI.ReadHarness [iterations] [path-to-xls]
// With no path, a 60,000 x 8 workbook (same shape as ReadFromBytesBenchmark "large") is generated
// once into the temp directory and reused.
int iterations = args.Length > 0 ? int.Parse(args[0]) : 10;
string path = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "npoi-large.xls");
if(!File.Exists(path))
{
    var gen = new HSSFWorkbook();
    ISheet s = gen.CreateSheet("big");
    for(int r = 0; r < 60_000; r++)
    {
        IRow row = s.CreateRow(r);
        for(int c = 0; c < 6; c++)
            row.CreateCell(c).SetCellValue($"r{r}c{c} text {r * 31 + c}");
        row.CreateCell(6).SetCellValue(r * 1.5);
        row.CreateCell(7).SetCellValue(r);
    }
    using var fo = File.Create(path);
    gen.Write(fo, true);
}

byte[] bytes = File.ReadAllBytes(path);
Console.WriteLine($"{path}: {bytes.Length / 1024.0 / 1024.0:F1} MB, {iterations} iterations, pid {Environment.ProcessId}");

var sw = Stopwatch.StartNew();
long peakWs = 0;
for(int i = 0; i < iterations; i++)
{
    using var wb = new HSSFWorkbook(new MemoryStream(bytes));
    long cells = 0;
    foreach(ISheet sheet in wb)
        foreach(IRow row in sheet)
            foreach(ICell cell in row)
                cells++;
    peakWs = Math.Max(peakWs, Process.GetCurrentProcess().PeakWorkingSet64);
    Console.WriteLine($"iter {i}: {cells} cells, {sw.ElapsedMilliseconds} ms elapsed, allocated {GC.GetTotalAllocatedBytes() / 1024 / 1024} MB total");
}
Console.WriteLine($"peak working set {peakWs / 1024 / 1024} MB, GC gen0/1/2 = {GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}");