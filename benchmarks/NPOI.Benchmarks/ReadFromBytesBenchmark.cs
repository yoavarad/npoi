using BenchmarkDotNet.Attributes;
using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.SS.UserModel;

namespace NPOI.Benchmarks;

/// <summary>
/// Read-path benchmarks (POIFS, HPSF, HSSF), each starting from a byte[] held in memory,
/// as ole-extractor does. Reports allocated bytes and GC counts via MemoryDiagnoser.
/// </summary>
[MemoryDiagnoser]
public class ReadFromBytesBenchmark
{
    private byte[] smallXls;
    private byte[] largeXls;
    private byte[] drawingsXls;
    private byte[] embeddedXls;
    private byte[] docBytes;

    [Params("small", "large")]
    public string Fixture { get; set; }

    private byte[] Xls => Fixture == "large" ? largeXls : smallXls;

    [GlobalSetup]
    public void Setup()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "data", "read");
        smallXls = File.ReadAllBytes(Path.Combine(dir, "SimpleWithImages.xls"));
        drawingsXls = File.ReadAllBytes(Path.Combine(dir, "Images.xls"));
        embeddedXls = File.ReadAllBytes(Path.Combine(dir, "WithEmbeddedObjects.xls"));
        docBytes = File.ReadAllBytes(Path.Combine(dir, "TestBug52117.doc"));
        largeXls = GenerateLargeXls();
    }

    // ~16 MB: many rows, many distinct strings (large SST), plus numeric cells.
    private static byte[] GenerateLargeXls()
    {
        var wb = new HSSFWorkbook();
        ISheet sheet = wb.CreateSheet("big");
        for(int r = 0; r < 60_000; r++)
        {
            IRow row = sheet.CreateRow(r);
            for(int c = 0; c < 6; c++)
                row.CreateCell(c).SetCellValue($"r{r}c{c} text {r * 31 + c}");
            row.CreateCell(6).SetCellValue(r * 1.5);
            row.CreateCell(7).SetCellValue(r);
        }
        using var ms = new MemoryStream();
        wb.Write(ms, true);
        Console.WriteLine($"Generated large xls: {ms.Length / 1024.0 / 1024.0:F1} MB");
        return ms.ToArray();
    }

    [Benchmark]
    public int NPOIFSFileSystem_OpenAndWalk()
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(Xls));
        return Walk(fs.Root);
    }

    [Benchmark]
    public int NPOIFSFileSystem_OpenAndWalk_ByteArray()
    {
        using var fs = new NPOIFSFileSystem(Xls);
        return Walk(fs.Root);
    }

    [Benchmark]
    public int NPOIFSFileSystem_OpenAndWalk_ExposableStream()
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(Xls, 0, Xls.Length, false, true));
        return Walk(fs.Root);
    }

    private static int Walk(DirectoryNode dir)
    {
        int n = 0;
        var it = dir.Entries;
        while(it.MoveNext())
        {
            n++;
            if(it.Current is DirectoryNode child)
                n += Walk(child);
        }
        return n;
    }

    [Benchmark]
    public int Hpsf_SummaryInformation()
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(docBytes));
        int n = 0;
        foreach(string name in new[] { SummaryInformation.DEFAULT_STREAM_NAME, DocumentSummaryInformation.DEFAULT_STREAM_NAME })
        {
            if(!fs.Root.HasEntry(name))
                continue;
            using var stream = fs.CreateDocumentInputStream(name);
            PropertySet ps = PropertySetFactory.Create(stream);
            n += ps.Sections.Count;
        }
        return n;
    }

    [Benchmark]
    public int HSSFWorkbook_Open()
    {
        using var wb = new HSSFWorkbook(new MemoryStream(Xls));
        return wb.NumberOfSheets;
    }

    [Benchmark]
    public int HSSFWorkbook_OpenAndIterateCells()
    {
        using var wb = new HSSFWorkbook(new MemoryStream(Xls));
        int cells = 0;
        foreach(ISheet sheet in wb)
            foreach(IRow row in sheet)
                foreach(ICell cell in row)
                {
                    _ = cell.CellType == CellType.String ? cell.StringCellValue : null;
                    cells++;
                }
        return cells;
    }

    [Benchmark]
    public int HSSFWorkbook_GetAllPictures()
    {
        using var wb = new HSSFWorkbook(new MemoryStream(drawingsXls));
        return wb.GetAllPictures().Count;
    }

    [Benchmark]
    public int HSSFWorkbook_GetAllEmbeddedObjects()
    {
        using var wb = new HSSFWorkbook(new MemoryStream(embeddedXls));
        return wb.GetAllEmbeddedObjects().Count;
    }
}