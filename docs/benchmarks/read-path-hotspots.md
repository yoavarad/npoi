# Read-path hotspot report (xls, byte[] -> HSSFWorkbook -> all cells)

Runbook: [../profiling.md](../profiling.md). Workload: `benchmarks/NPOI.ReadHarness`, generated `large` fixture (60,000 rows x 8 cols, 16.3 MB, large SST), open from `byte[]` then iterate every cell (480,000 cells), 8 iterations for CPU and counters, 3 for allocations.
Environment: Windows 11, .NET 10.0.401 SDK, Release, tag after task #79 (NDocumentInputStream scratch buffer) was merged. Sampled data: percentages are reliable, sub-1% rows are not.

## Memory utilization (dotnet-counters, 8 iterations)

| Metric | Value |
|---|---|
| Peak working set | 819 MB (harness-reported peak: 450 MB over 2 iterations) |
| Peak committed | 781 MB |
| Gen2 heap (last collection, max) | 538 MB |
| LOH (last collection, max) | 219 MB |
| Allocation rate (max) | 352 MB/s |
| Allocated per iteration | about 340 MB (harness: 769 MB after 1, 1109 MB after 2) for a 16.3 MB file, roughly 21x |
| GC collections over the run | gen0 18, gen1 129, gen2 8 (per-second counts summed) |
| User CPU over the run | 12 s, GC pause counter sums to about 5 s |

About a quarter of sampled wall time is spent in GC suspension (see CPU row 1).

## Top 10 CPU (dotnet-sampled-thread-time, exclusive %)

| # | Method | Excl. | Incl. |
|--:|---|--:|--:|
| 1 | `Thread.PollGCWorker` (GC suspension, i.e. GC cost) | 25.1% | 25.1% |
| 2 | `RecordInputStream.ReadStringCommon` | 16.8% | 20.3% |
| 3 | `SSTDeserializer.ManufactureStrings` | 9.9% | 33.1% |
| 4 | `NDocumentInputStream.ReadFully` | 5.9% | 6.7% |
| 5 | `HSSFSheet.SetPropertiesFromSheet` (eager row/cell build) | 4.5% | 32.0% |
| 6 | `HSSFRow.GetSortedCells` | 4.0% | 6.3% |
| 7 | `CastHelpers.IsInstanceOfClass` | 3.1% | 3.1% |
| 8 | `RecordInputStream..ctor` | 2.1% | 5.0% |
| 9 | generic collection `Resize` (growth) | 2.0% | 8.0% |
| 10 | `NDocumentInputStream.ReadUShort` | 1.7% | 4.8% |

Inclusive context: `RecordFactory.CreateRecords` 51.7% (record parsing), `RuntimeConstructorInfo.Invoke` 44.0% (reflection wrapper around the record constructors, mostly the parsing inside them), `RecordFactoryInputStream.ReadNextRecord` 47.3%. `RecordFactory` reflection itself (`Invoke` exclusive) is 0.7%.

## Top 10 allocations (gc-verbose, 3 iterations, about 1,036 MB sampled)

By type:

| # | Type | Share | MB |
|--:|---|--:|--:|
| 1 | `Entry<int, ICell>[]` (row cell dictionary buckets) | 12.0% | 124.6 |
| 2 | `object[]` | 10.1% | 104.4 |
| 3 | `HSSFCell` | 9.6% | 99.7 |
| 4 | `char[]` | 6.9% | 71.7 |
| 5 | `byte[]` | 6.3% | 65.4 |
| 6 | `string` | 6.3% | 64.9 |
| 7 | `int[]` | 6.0% | 61.7 |
| 8 | `Record[]` | 5.0% | 52.1 |
| 9 | `Entry<UnicodeString, int>[]` | 5.0% | 51.7 |
| 10 | `UnicodeString` | 4.7% | 48.5 |

By innermost NPOI frame:

| # | Frame | Share | MB |
|--:|---|--:|--:|
| 1 | `HSSFSheet.SetPropertiesFromSheet` (HSSFRow/HSSFCell/dictionaries) | 27.9% | 289.4 |
| 2 | `SSTDeserializer.ManufactureStrings` | 13.3% | 138.1 |
| 3 | `RecordInputStream.ReadStringCommon` | 13.2% | 136.6 |
| 4 | `HSSFRow.GetSortedCells` | 10.9% | 112.7 |
| 5 | `RecordFactory.ReflectionConstructorRecordCreator.Create` (record objects) | 10.1% | 104.6 |
| 6 | `ByteBuffer..ctor` (full-stream copy in NPOIFSFileSystem) | 4.7% | 48.9 |
| 7 | `RecordFactory.CreateRecords` (record list growth) | 4.6% | 48.0 |
| 8 | `RowBlocksReader..ctor` | 4.6% | 48.0 |
| 9 | `HSSFCell..ctor` | 3.9% | 40.9 |
| 10 | `ValueRecordsAggregate.InsertCell` | 2.0% | 20.7 |

## Verdict on the suspected hotspots

| Suspect | Finding |
|---|---|
| Per-character string decoding in RecordInputStream | Confirmed, top CPU item: `ReadStringCommon` + `ManufactureStrings` = 26.7% exclusive, 33% inclusive, and about 26% of allocations (char[], string, UnicodeString). |
| Eager HSSFRow/HSSFCell construction | Confirmed, biggest allocator: `SetPropertiesFromSheet` + `GetSortedCells` + `HSSFCell..ctor` are about 43% of allocated bytes and 32% inclusive CPU. Also the main source of GC cost (LOH/gen2 churn). |
| Full-stream copy in NPOIFSFileSystem(Stream) | Confirmed but modest for CPU: `ByteBuffer..ctor` is 4.7% of allocations (one 16 MB LOH array per open). Worth doing for LOH pressure, not for CPU. |
| NDocumentInputStream per-read byte[] allocations | Already fixed by #79; `ReadFully` remains 5.9% exclusive (stream copying, not allocation). No further work indicated. |
| RecordFactory reflection | Not a CPU hotspot (0.7% exclusive for the reflective invoke). It is an allocation source (10.1%, but that is the record objects themselves, which static constructors would not avoid). Lowest priority. |

Not among the suspects but visible: `HSSFRow.GetSortedCells` rebuilds a sorted array per row (4.0% CPU, 10.9% allocations).

## Re-ranked candidate tasks

1. #80 bulk string decoding / SST: top CPU (about 27% exclusive) and about a quarter of allocations.
2. #84 HSSFWorkbook load: allocation-heavy collections and eager row/cell objects: about 43% of allocations, plus GetSortedCells. The O(n^2) label conversion was not visible at this size and should be re-checked with a larger label count.
3. #81 avoid full-stream copy: 4.7% of allocations but removes the largest single LOH array per open; cheap and low-risk.
4. #82 RecordFactory reflection: 0.7% CPU; lowest value. Suggest deprioritizing or closing.
5. #83 HPSF PropertySet buffer: not exercised by this workload (xls body read); the HPSF benchmark shows 125 KB per op, negligible. Not validated by this profile; keep low.
