# IDisposable / using audit (task #39)

Scope: `main/NPOI.Core.csproj` (POIFS, HPSF, HSSF, Util, DDF, SS), the part ole-extractor uses.
`ooxml/` and `openxml4Net/` were not audited (follow-up task). `POIFS/Crypt/` is owned by task #41.

Method: a local build of NPOI.Core (net10.0) with CA2000 and CA1001 switched on as warnings (the
config change was not committed), a grep for every place that opens an OS file handle
(`new FileStream`, `File.Open*`, `FileInfo.Open*`), and a read-only sweep of finalizers, stream-owning
fields and `Dispose` idempotency.

Key fact: POIFS never keeps a file handle after construction. `NPOIFSFileSystem(FileInfo)` and
`FileBackedDataSource` copy the whole file into a `MemoryStream` and close the file. So once
construction succeeds, only extractors that keep a raw BIFF stream (`OldExcelExtractor`) still hold
a handle.

## Fixed in this PR

| Site | Problem | Fix | Test |
|---|---|---|---|
| `POIFS/NIO/FileBackedDataSource.cs` `(FileInfo, bool)` ctor | Opened a `FileStream`, copied it and never closed it. The handle stayed open until GC, so the file could not be deleted | `using` around the `FileStream` | `FileBackedDataSourceFromFileDoesNotHoldHandle` (failed before the fix) |
| `POIFS/FileSystem/NPOIFSFileSystem.cs` (and `POIFSFileSystem`, which inherits it) | Had `Close()` but no `IDisposable`, so it could not be used in `using` (#40 review) | Implements `IDisposable`; `Dispose()` calls `Close()`. Calling it twice is safe | `NPOIFSFileSystemUsingReleasesFile`, `POIFSFileSystemUsingReleasesFile`, `POIFSFileSystemDisposeIsIdempotent`, `HSSFWorkbookFromFileReleasesFile` |
| `POITextExtractor.cs` (base of `ExcelExtractor`, `EventBasedExcelExtractor`, `HPSFPropertiesExtractor` and the OOXML extractors) | Owns `fsToClose` but was only `ICloseable` | Implements `IDisposable`; `Dispose()` calls `Close()` | `ExtractorsUsingReleasesFile` |
| `HSSF/Extractor/OldExcelExtractor.cs` | Keeps the file's `FileStream` open for non-OLE2 BIFF files, but was not `IDisposable`. `Close()` also set `toClose = null` where it meant `toCloseStream = null` | Implements `IDisposable`; nulls the right field | `OldExcelExtractorUsingReleasesNonOle2File` |

## CA2000 / CA1001 findings reviewed (NPOI.Core, net10.0: 44 unique)

Deferred items are marked; everything else is harmless because it only wraps in-memory data.

| Finding | Verdict |
|---|---|
| CA2000 `POIFS/NIO/FileBackedDataSource.cs:59` `new FileStream` | **Real leak - fixed** |
| CA2000 `LittleEndianByteArrayOutputStream` / `LittleEndianInputStream` / `LittleEndianOutputStream` over byte arrays or `MemoryStream`: `HPSF/ClipboardData.cs:44`, `HPSF/PropertySetFactory.cs:96`, `HSSF/Record/AutoFilter/DOPER.cs:140`, `HSSF/Record/Cont/ContinuableRecord.cs:67`, `HSSF/Record/EmbeddedObjectRefSubRecord.cs:188`, `HSSF/Record/FilePassRecord.cs:84`, `HSSF/Record/ObjRecord.cs:96,172`, `HSSF/Record/StandardRecord.cs:53`, `HSSF/Record/SubRecord/SubRecord.cs:72`, `HSSF/UserModel/HSSFWorkbook.cs:1548,1549`, `POIFS/FileSystem/Ole10Native.cs:400,407`, `SS/Formula/PTG/Ptg.cs:303`, `SS/Util/CellRangeAddressList.cs:126`, `HSSF/Record/Crypto/Biff8EncryptionKey.cs:134`, `POIFS/Crypt/DataSpaceMapUtils.cs:69` | Harmless: in-memory, no handle. The returned or wrapped stream is sometimes caller-owned, so adding `using` could close a stream the caller still needs |
| CA2000 `ByteArrayInputStream`, `ByteArrayOutputStream`, `BoundedInputStream`, `StringWriter`: `HSSF/UserModel/HSSFWorkbook.cs:1338`, `HPSF/PropertySet.cs:596`, `HPSF/Util.cs:157`, `POIFS/FileSystem/OPOIFSDocument.cs:417`, `Util/IOUtils.cs:120,122,128` | Harmless: in-memory |
| CA2000 `DocumentInputStream` from an in-memory POIFS: `HSSF/UserModel/HSSFWorkbook.cs:316`, `HSSF/EventUserModel/HSSFEventFactory.cs:52`, `POIFS/FileSystem/Ole10Native.cs:97`, `POIFS/Crypt/EncryptionInfo.cs:90`, `POIFS/Crypt/CryptoAPI/CryptoAPIEncryptor.cs:132` | Harmless: reads blocks of the in-memory filesystem |
| CA2000 enumerators: `HSSF/Record/Aggregates/ValueRecordsAggregate.cs:267,381`, `HSSF/UserModel/HSSFSheet.cs:182`, `POIFS/FileSystem/NPOIFSMiniStore.cs:66`, `POIFS/property/NPropertyTable.cs:44` | Harmless: the enumerator is returned or iterated, and holds nothing |
| CA2000 `DDF/EscherMetafileBlip.cs:209` `InflaterInputStream` over a `MemoryStream` | Harmless: in-memory (the inflater's native buffer is managed by SharpZipLib) |
| CA2000 `HSSF/Record/Crypto/Biff8DecryptingStream.cs:50` `PushbackInputStream` | Harmless: it wraps the caller's stream and is kept as a field |
| CA2000 `POIFS/Crypt/CryptoAPI/CryptoAPIDecryptor.cs:245`, `CryptoAPIEncryptor.cs:111` | **Deferred** to task #41 (crypto) |
| CA2000 `SS/Util/SheetUtil.cs:958` `new SKFontStyle(...)` | **Deferred**: SkiaSharp native object (column auto-size), not on the extraction path |
| CA2000 `Util/Collections/Properties.cs:159` `StreamReader` over the caller's stream | Harmless: disposing the reader would close the caller's stream |
| CA1001 `HPSF/Section.cs` field `sectionBytes` (`ByteArrayOutputStream`) | Harmless: in-memory buffer |
| CA1001 `POIFS/FileSystem/NPOIFSStream.cs` field `outStream` | Harmless: in-memory. **Deferred**: making it `IDisposable` would change the public API for no gain in releasing handles |

## Other sites found by the sweep

| Site | Verdict |
|---|---|
| `POIFS/NIO/FileBackedDataSource.cs` finalizer | Correct: `Dispose(false)` touches nothing managed |
| `NPOIFSFileSystem(FileStream, FileInfo, bool, bool)` | Closes the channel it opened in `finally`. It also closes a channel the caller passed in, which is existing POI behaviour and left as is |
| `HSSF/Extractor/EventBasedExcelExtractor.cs` field `fs` | Not closed by the extractor. The filesystem belongs to the caller, and it holds no handle (in-memory). Left as is |
| `POIFS/Macros/VBAMacroReader.cs` | `ICloseable` but not `IDisposable`. `VBAMacroReader(FileInfo)` in the OOXML branch hands `file.OpenRead()` to a `ZipInputStream` that is closed on the normal paths. **Deferred** (not on the ole-extractor path) |
| `POIDocument` (base of `HSSFWorkbook`) | `HSSFWorkbook` is already `IDisposable` through `IWorkbook`; `Dispose()` calls `Close()`, which closes the `NPOIFSFileSystem` it was opened from |
| `Util/HexRead.cs:46`, `POIFS/FileSystem/POIFSFileSystem.cs:143`, `POIFS/Dev/*`, `DocumentFactoryHelper.cs:95`, `Util/TempFile.cs:37` | Closed with `try/finally` or `using` |
| `POIFS/Crypt/Standard/StandardEncryptor.cs:119`, `POIFS/Crypt/ChunkedCipherOutputStream.cs:70` | **Deferred** to task #41 (crypto) |