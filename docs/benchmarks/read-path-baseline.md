# Read-path baseline (POIFS / HPSF / HSSF, from byte[])

Benchmark class: `benchmarks/NPOI.Benchmarks/ReadFromBytesBenchmark.cs`. Every case starts from a `byte[]` held in memory, as ole-extractor does.
Later read-path tasks compare against these numbers (allocated bytes and Gen0/1/2 counts matter most; timings are `--job short` and noisy).

Environment: BenchmarkDotNet 0.13.12, Windows 11, .NET 10.0.12 X64 RyuJIT AVX2, `--job short` (3 warmup, 3 iterations, 1 launch).

Fixtures:
- `small`: `testcases/test-data/spreadsheet/SimpleWithImages.xls`.
- `large`: generated in setup, 60,000 rows x 8 columns (6 unique-string columns, so a large SST), 16.3 MB.
- HPSF: `testcases/test-data/hpsf/TestBug52117.doc`. Pictures: `Images.xls`. Embedded objects: `WithEmbeddedObjects.xls`. These three do not vary with `Fixture`, so their large rows repeat the small ones.

| Method                             | Fixture | Mean             | Error          | StdDev         | Gen0       | Gen1       | Gen2       | Allocated    |
|----------------------------------- |-------- |-----------------:|---------------:|---------------:|-----------:|-----------:|-----------:|-------------:|
| NPOIFSFileSystem_OpenAndWalk       | large   |     4,809.312 us |   3,621.468 us |    198.5049 us |   359.3750 |   343.7500 |   304.6875 |  16958.07 KB |
| Hpsf_SummaryInformation            | large   |        29.000 us |      12.885 us |      0.7063 us |    10.0098 |     2.4414 |          - |    125.38 KB |
| HSSFWorkbook_Open                  | large   | 1,101,151.400 us | 321,338.254 us | 17,613.6340 us | 59000.0000 | 29000.0000 | 12000.0000 | 667737.29 KB |
| HSSFWorkbook_OpenAndIterateCells   | large   | 1,210,167.033 us | 270,681.042 us | 14,836.9413 us | 59000.0000 | 30000.0000 |  9000.0000 | 708516.38 KB |
| HSSFWorkbook_GetAllPictures        | large   |       133.012 us |     380.986 us |     20.8831 us |    22.4609 |     5.8594 |          - |    286.27 KB |
| HSSFWorkbook_GetAllEmbeddedObjects | large   |       541.089 us |     620.099 us |     33.9897 us |    42.9688 |    11.7188 |          - |    734.91 KB |
| NPOIFSFileSystem_OpenAndWalk       | small   |         7.893 us |      11.142 us |      0.6107 us |     6.0883 |     1.5106 |          - |     75.04 KB |
| Hpsf_SummaryInformation            | small   |        26.882 us |      12.874 us |      0.7057 us |    10.0098 |     2.4414 |          - |    125.38 KB |
| HSSFWorkbook_Open                  | small   |        62.064 us |      41.965 us |      2.3002 us |    15.5029 |     3.7842 |          - |    190.34 KB |
| HSSFWorkbook_OpenAndIterateCells   | small   |        80.189 us |     147.778 us |      8.1002 us |    15.5029 |     3.8452 |          - |    190.44 KB |
| HSSFWorkbook_GetAllPictures        | small   |       112.027 us |      34.274 us |      1.8787 us |    22.9492 |     5.8594 |          - |    286.27 KB |
| HSSFWorkbook_GetAllEmbeddedObjects | small   |       448.442 us |     649.591 us |     35.6063 us |    42.9688 |    11.7188 |          - |    734.91 KB |

Columns: Gen0/Gen1/Gen2 are collections per 1000 ops; Allocated is managed bytes per op.
ThreadingDiagnoser is not used: BenchmarkDotNet 0.13.12 rejects it in this setup ("supports only .NET Core 3.0+").
