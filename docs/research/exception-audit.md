# Exception-handling audit (task #37)

Scope: `main`, `ooxml`, `openxml4Net`. Sweep patterns: empty catch bodies (`catch ... { }`), `catch (Exception ...)`, bare `catch`.
Line numbers refer to the base commit `948fa6b3f` (before this change).

Decisions: **fixed** (changed in this PR), **leave** (correct as-is, reason given), **comment** (kept, justifying comment added),
**deferred** (should be narrowed/logged/rethrown; tracked in follow-up task #69).

## Empty catch blocks (16) — all resolved

| File:line | Decision | Notes |
|---|---|---|
| main/POIFS/Storage/BlockAllocationTableReader.cs:233 | fixed | Dead catch: `_entries` is `List<int>`, which throws `ArgumentOutOfRangeException`, not `IndexOutOfRangeException`. `IsUsed` now returns `false` for out-of-range indices (upstream contract) and `GetNextBlockIndex` throws `IOException("index N is unused")`. Test: `TestIsUsedOutOfRangeIndex`. |
| main/POIFS/Crypt/Standard/StandardEncryptor.cs:166 | fixed | `DoFinal` failure (PKCS5 padding, should not fail) was swallowed and produced a truncated package; now propagates. |
| main/POIFS/Crypt/Standard/StandardDecryptor.cs:341 | comment | Draining unread data on `Close` is optional; errors in discarded data must not fail `Close`. |
| main/Common/UserModel/Fonts/FontCharset.cs:178 | fixed (narrowed) | `ArgumentException`/`NotSupportedException` only: unknown charset leaves `Charset` null (upstream parity). |
| main/Util/InputStream.cs:184 | comment | java.io.InputStream contract: an IOException after the first byte ends the read early. |
| main/Util/POILogFactory.cs:92 | comment | Unreadable app config falls back to the environment variable. |
| main/HSSF/UserModel/StaticFontMetrics.cs:68 | comment | Unreadable app config uses embedded metrics. |
| main/HSSF/UserModel/StaticFontMetrics.cs:98 | comment | Close error after load changes nothing. |
| main/SS/Formula/Functions/DateValue.cs:97 | comment | Non-numeric month falls through to month-name lookup. |
| ooxml/POIXMLProperties.cs:208 | comment | Non-numeric revision ignored (upstream parity). |
| ooxml/XSSF/UserModel/BaseXSSFEvaluationWorkbook.cs:95 | comment | Non-numeric book name resolved via external link table. |
| ooxml/XSSF/Streaming/SheetDataWriter.cs:126 | comment + deferred | Best-effort close kept; upstream propagates close errors. Changing it overlaps the IDisposable audit (#39). |
| ooxml/XDDF/UserModel/Chart/XDDFChart.cs:1024, 1027 | comment | Missing/invalid embedded workbook yields null sheet (upstream parity). Unused exception variables removed. |
| openxml4Net/OPC/Internal/PackagePropertiesPart.cs:625, 639 | comment | Try-next-date-format loop. |

## Swallowing catches with commented-out logging (HPSF) — fixed

Upstream Apache POI logs these; NPOI had the log calls commented out, so failures vanished silently. Logging restored
(no control-flow change).

| File:line | Decision |
|---|---|
| main/HPSF/Property.cs:336 (`catch (SystemException)` in dictionary read) | fixed: WARN log |
| main/HPSF/Property.cs:534 (`ToString` serialization) | fixed: WARN log |
| main/HPSF/Property.cs:629 (`DecodeValueFromID`) | fixed: WARN log |
| main/HPSF/Section.cs:246 (dictionary fallback) | fixed: INFO log |
| main/HPSF/Section.cs:964 (bogus dictionary entry) | fixed: WARN log |

## Broad catches on ole-extractor paths (POIFS / HPSF / HSSF / DDF / Util)

| File:line | Pattern | Decision | Reason |
|---|---|---|---|
| main/HSSF/Record/RecordFactory.cs:77 | E | fixed | A `RecordFormatException` thrown by a record constructor now propagates as-is instead of being re-wrapped as "Unable to construct record instance"; other failures keep a non-null cause (`e.InnerException ?? e`). Exception type unchanged. Test: `TestRecordFormatExceptionFromConstructorIsNotRewrapped`. |
| main/HSSF/Record/RecordFactory.cs:106 | E | fixed | Same as above for `Create` factory methods. |
| main/HSSF/Record/RecordFactory.cs:614 | E | leave | Wraps with cause. |
| main/HSSF/Record/RecordFactory.cs:661 | B | leave | Documented fall-through to `Create` method lookup. |
| main/HSSF/Record/RecordFactory.cs:670 | B | deferred | Throws without cause (startup-time reflection only). |
| main/Util/StringUtil.cs:153 | B | fixed | Removed catch-all that replaced any failure with a message-less `InvalidOperationException`. Arguments are pre-validated, so unreachable in practice. |
| main/Util/StringUtil.cs:253 | B | fixed | Same; `PutUnicodeBE` into a too-small buffer now surfaces `ArgumentException`. Test: `TestPutUnicodeBEOutputTooSmall`. |
| main/POIFS/Storage/BlockAllocationTableReader.cs:192 | E | leave | Two logged special cases, otherwise `throw;`. |
| main/POIFS/FileSystem/DocumentFactoryHelper.cs:259 | E | leave | `ArgumentException` maps to "not protected", everything else rethrown. |
| main/POIDocument.cs:270 | E | leave | Logs WARN and returns null (upstream parity). |
| main/POIDocument.cs:350 | E when | leave | Already filtered; wraps with cause. |
| main/HPSF/Section.cs:585 | E | leave | Wraps in `HPSFRuntimeException` with cause. |
| main/HSSF/Record/Crypto/Biff8DecryptingStream.cs:64 | E | leave | Wraps in `RecordFormatException` with cause. |
| main/HSSF/UserModel/HSSFWorkbook.cs:1599, 1651 | E | leave | Wrap in `EncryptedDocumentException` with cause. |
| main/HSSF/Record/Aggregates/Chart/AxisParentAggregate.cs:52 | B | deferred | Chart axes parse failure only `Debug.Print`ed; should narrow + log. |
| main/HSSF/Util/HSSFCellUtil.cs:164 | B | deferred | Narrow to `InvalidOperationException`. |
| main/HSSF/Util/RangeAddress.cs:328 | E | deferred | Narrow to `FormatException`/`OverflowException`. |
| main/HSSF/Record/AbstractEscherHolderRecord.cs:47 | E | leave | Commented-out code. |
| main/HSSF/UserModel/StaticFontMetrics.cs:68 | E | comment | See empty-catch table. |
| main/DDF/DefaultEscherRecordFactory.cs:115 | B | leave | Unknown record fallback (upstream parity). |
| main/DDF/DefaultEscherRecordFactory.cs:146 | B | deferred | Static init; wraps without cause. |
| main/DDF/DefaultEscherRecordFactory.cs:156 | E | leave | Wraps with cause. |
| main/DDF/EscherBSERecord.cs:357, EscherBlipRecord.cs:155, EscherBlipWMFRecord.cs:313, 345, EscherComplexProperty.cs:170, EscherTextboxRecord.cs:178, 200, UnknownEscherRecord.cs:211 | E/B | leave | Diagnostic `ToString`/XML dumps must not throw; error text is embedded in the output. |
| main/DDF/EscherDgRecord.cs:127, EscherDggRecord.cs:427, EscherSpgrRecord.cs:138, EscherSplitMenuColorsRecord.cs:147 | E | leave | Commented-out code. |
| main/Common/UserModel/Fonts/FontCharset.cs:178 | E | fixed | See empty-catch table. |
| main/Util/CodePageUtil.cs:223 | E when | leave | Already filtered; maps to `UnsupportedEncodingException`. |
| main/Util/IOUtils.cs:442, 459 | E | leave | `CloseQuietly` contract; logs ERROR. |
| main/Util/POILogFactory.cs:92 | E | comment | See empty-catch table. |
| main/Util/POILogFactory.cs:131 | E | leave | Logger bootstrap falls back to null logger (commented). |
| main/Util/SystemOutLogger.cs:97 | B | leave | Bad level config falls back to DEBUG. |

> Update (task #123): every row marked narrow/log/rethrow below, plus the deferred HSSF/DDF rows above, is now resolved. SheetDataWriter.Close() propagates errors.

## Remaining broad catches (non-priority areas)

Pattern: E = `catch(Exception)`, B = bare `catch`, E when = filtered. Items marked narrow/log/rethrow are deferred to #69.

### POIFS/Crypt (main)
| File:line | Pattern | Decision | Reason |
|---|---|---|---|
| main/POIFS/Crypt/BinaryRC4/BinaryRC4Decryptor.cs:78 | E | leave | Wraps in EncryptedDocumentException with cause |
| main/POIFS/Crypt/BinaryRC4/BinaryRC4Encryptor.cs:116 | E | leave | Wraps with message and cause |
| main/POIFS/Crypt/ChunkedCipherOutputStream.cs:289 | E | rethrow | new IOException(e.Message) drops inner exception; pass cause |
| main/POIFS/Crypt/CryptoAPI/CryptoAPIDecryptor.cs:66 | E | leave | Wraps with cause |
| main/POIFS/Crypt/CryptoAPI/CryptoAPIDecryptor.cs:82 | E | leave | Wraps with cause |
| main/POIFS/Crypt/CryptoAPI/CryptoAPIDecryptor.cs:137 | E | leave | Wraps with cause |
| main/POIFS/Crypt/CryptoAPI/CryptoAPIEncryptor.cs:71 | E | leave | Wraps with cause |
| main/POIFS/Crypt/CryptoAPI/CryptoAPIEncryptor.cs:301 | E | leave | Wraps with cause |
| main/POIFS/Crypt/CryptoAPI/CryptoAPIEncryptor.cs:314 | E | leave | Wraps with cause |
| main/POIFS/Crypt/CryptoFunctions.cs:116 | E | leave | Wraps with cause |
| main/POIFS/Crypt/CryptoFunctions.cs:267 | E | leave | Wraps with cause |
| main/POIFS/Crypt/CryptoFunctions.cs:324 | E | leave | Wraps with cause |
| main/POIFS/Crypt/CryptoFunctions.cs:344 | E | leave | Wraps with cause |
| main/POIFS/Crypt/CryptoFunctions.cs:359 | E | leave | commented-out (registerBouncyCastle body) |
| main/POIFS/Crypt/EncryptionInfo.cs:148 | E | leave | Wraps as IOException with cause |
| main/POIFS/Crypt/EncryptionInfo.cs:290 | E | leave | Wraps with cause |
| main/POIFS/Crypt/Standard/StandardDecryptor.cs:70 | E | leave | Wraps with cause |
| main/POIFS/Crypt/Standard/StandardDecryptor.cs:220 | B | comment | [MS-OFFCRYPTO] allows arbitrary padding bytes; tail beyond StreamSize is discarded |
| main/POIFS/Crypt/Standard/StandardEncryptor.cs:82 | E | leave | Wraps with cause |

### SS/Format + Formula
| File:line | Pattern | Decision | Reason |
|---|---|---|---|
| main/SS/Format/CellFormat.cs:193 | B | log | Invalid format part silently becomes null; log warning (log call commented out) |
| main/SS/Format/CellNumberFormatter.cs:872 | E | log | Prints stack to Console and swallows; use logger, narrow to FormatException |
| main/SS/Format/CellNumberStringMod.cs:84 | B | narrow | Equals cast/null; use `is` check instead of catch |
| main/SS/Formula/Atp/Switch.cs:27 | B | narrow | EvaluationException to its ErrorEval; others should propagate |
| main/SS/Formula/Atp/XLookupFunction.cs:72 | E | narrow | ArgumentException/InvalidCastException from GetMatchMode |
| main/SS/Formula/Atp/XLookupFunction.cs:90 | B | narrow | ArgumentException from GetSearchMode |
| main/SS/Formula/Atp/XMatchFunction.cs:47 | B | narrow | ArgumentException from GetMatchMode |
| main/SS/Formula/Atp/XMatchFunction.cs:65 | B | narrow | ArgumentException from GetSearchMode |
| main/SS/Formula/Eval/Forked/ForkedEvaluator.cs:66 | E | leave | Reflection failure wrapped with cause |
| main/SS/Formula/Eval/OperandResolver.cs:282 | B | narrow | double.Parse: FormatException/OverflowException |
| main/SS/Formula/Eval/OperandResolver.cs:314 | B | leave | commented-out code |
| main/SS/Formula/EvaluationConditionalFormatRule.cs:737 | B | narrow | cv.String failure: InvalidOperationException/EvaluationException |
| main/SS/Formula/EvaluationConditionalFormatRule.cs:749 | B | narrow | cv.String failure: InvalidOperationException/EvaluationException |
| main/SS/Formula/Functions/Areas.cs:28 | B | narrow | InvalidCastException/NullReferenceException on arg type; maps to #VALUE! |
| main/SS/Formula/Functions/Averageif.cs:56 | B | narrow | Only IndexOutOfRangeException expected; better a length check |
| main/SS/Formula/Functions/Days.cs:52 | E | narrow | EvaluationException; fallback then parses text date |
| main/SS/Formula/Functions/NumberValueFunction.cs:94 | B | narrow | FormatException/OverflowException after EvaluationException |
| main/SS/Formula/Functions/Sheet.cs:64 | E | leave | Spreadsheet fallback to #VALUE! by design |
| main/SS/Formula/Functions/Sumproduct.cs:151 | B | narrow | Array.Copy: InvalidCastException/ArrayTypeMismatchException |
| main/SS/Formula/Functions/Text/Text.cs:50 | B | narrow | FormatRawCellContents: FormatException/ArgumentException to #VALUE! |
| main/SS/Formula/Functions/Text/Text.cs:140 | E | leave | commented-out code |
| main/SS/Formula/Functions/WeekNum.cs:70 | E | narrow | GetJavaDate: ArgumentException family to #NUM! |
| main/SS/Formula/WorkbookEvaluator.cs:529 | B | leave | Deliberate: preserve original exception while building message |

### SS/UserModel + Util
| File:line | Pattern | Decision | Reason |
|---|---|---|---|
| main/SS/UserModel/DataFormatter.cs:358 | E | leave | Logs warning and falls back by design |
| main/SS/UserModel/DataFormatter.cs:1186 | E | narrow | CachedFormulaResultType: InvalidOperationException only |
| main/SS/Util/SheetUtil.cs:393 | B | leave | Font measure fallback by design |
| main/SS/Util/SheetUtil.cs:440 | B | leave | Format failure falls back to raw numeric string |
| main/SS/Util/SheetUtil.cs:478 | E | leave | Documented font-support fallback |
| main/SS/Util/SheetUtil.cs:554 | E | leave | Documented font-support fallback |
| main/SS/Util/SheetUtil.cs:628 | B | leave | Format failure falls back to raw numeric string |
| main/SS/Util/SheetUtil.cs:744 | E | leave | Documented font-support fallback (width 7) |

### ooxml crypt/Dsig
| File:line | Pattern | Decision | Reason |
|---|---|---|---|
| ooxml/POIFS/Crypt/Agile/AgileDecryptor.cs:266 | E | leave | Wraps with cause |
| ooxml/POIFS/Crypt/Agile/AgileEncryptionHeader.cs:45 | B | rethrow | Wraps without inner exception; catch NullReferenceException, attach cause |
| ooxml/POIFS/Crypt/Agile/AgileEncryptionInfoBuilder.cs:148 | E | leave | Wraps with cause |
| ooxml/POIFS/Crypt/Agile/AgileEncryptionVerifier.cs:62 | E | leave | Wraps with cause |
| ooxml/POIFS/Crypt/Agile/AgileEncryptionVerifier.cs:119 | E | leave | Wraps with cause |
| ooxml/POIFS/Crypt/Agile/AgileEncryptor.cs:171 | E | leave | Wraps with cause |
| ooxml/POIFS/Crypt/Agile/AgileEncryptor.cs:313 | E | leave | Wraps with cause |
| ooxml/POIFS/Crypt/Agile/AgileEncryptor.cs:365 | E when | leave | Filter already narrow (IOException/XmlException); wraps with cause |
| ooxml/POIFS/Crypt/Dsig/Facets/SignatureFacet.cs:164 | E | leave | commented-out code |
| ooxml/POIFS/Crypt/Dsig/Facets/XAdESXLSignatureFacet.cs:207 | E | leave | commented-out code |
| ooxml/POIFS/Crypt/Dsig/Facets/XAdESXLSignatureFacet.cs:266 | E | leave | commented-out code |
| ooxml/POIFS/Crypt/Dsig/Facets/XAdESXLSignatureFacet.cs:268 | E | leave | commented-out code |
| ooxml/POIFS/Crypt/Dsig/Facets/XAdESXLSignatureFacet.cs:319 | E | leave | commented-out code |
| ooxml/POIFS/Crypt/Dsig/Services/RelationshipTransformService.cs:153 | E | leave | Wraps in TransformException with cause |
| ooxml/POIFS/Crypt/Dsig/SignatureConfig.cs:954 | E | leave | commented-out code (try-next-provider loop) |
| ooxml/POIFS/Crypt/Dsig/SignatureInfo.cs:204 | E | leave | commented-out code |
| ooxml/POIFS/Crypt/Dsig/SignatureInfo.cs:294 | E | leave | commented-out code |
| ooxml/POIFS/Crypt/Dsig/SignatureInfo.cs:365 | E | leave | commented-out code |
| ooxml/POIFS/Crypt/Dsig/SignatureInfo.cs:595 | E | leave | commented-out code |

### ooxml other
| File:line | Pattern | Decision | Reason |
|---|---|---|---|
| ooxml/POIXMLDocument.cs:131 | E | leave | Wraps in POIXMLException with cause |
| ooxml/POIXMLDocumentPart.cs:727 | E | leave | Specific rethrow first, then wraps with cause |
| ooxml/POIXMLFactory.cs:71 | E | leave | Wraps with cause |
| ooxml/POIXMLFactory.cs:123 | E | leave | Wraps with cause |
| ooxml/SS/UserModel/WorkbookFactory.cs:239 | B | leave | Cleanup then `throw;` |
| ooxml/XSSF/Extractor/EmbeddedExtractor.cs:127 | E | leave | Logs warning and ignores embedding by design |
| ooxml/XSSF/Model/StylesTable.cs:218 | B | narrow | Enum.Parse: ArgumentException/OverflowException |
| ooxml/XSSF/Streaming/SXSSFSheet.cs:595 | E | leave | Wraps with cause |
| ooxml/XSSF/Streaming/SheetDataWriter.cs:91 | B | leave | Closes stream then `throw;` |
| ooxml/XSSF/Streaming/SheetDataWriter.cs:518 | E | log | Delete failure swallowed silently; log, narrow to IOException/UnauthorizedAccessException |
| ooxml/XSSF/UserModel/XSSFDrawing.cs:776 | B | narrow | Null/index probe: NullReferenceException/IndexOutOfRangeException (or null checks) |
| ooxml/XSSF/UserModel/XSSFSheet.cs:4560 | B | narrow | XElement.Parse: XmlException/NullReferenceException; only Debug.WriteLine |
| ooxml/XSSF/UserModel/XSSFSheet.cs:6450 | B | narrow | NumericCellValue on non-numeric: InvalidOperationException |
| ooxml/XSSF/UserModel/XSSFWorkbook.cs:498 | E | leave | Wraps in POIXMLException with cause |
| ooxml/XWPF/Usermodel/XWPFDocument.cs:210 | E | leave | Wraps with cause |
| ooxml/XWPF/Usermodel/XWPFDocument.cs:299 | E | leave | Wraps with cause |
| ooxml/XWPF/Usermodel/XWPFDocument.cs:565 | E | leave | Wraps with cause |
| ooxml/XWPF/Usermodel/XWPFDocument.cs:669 | E | leave | Wraps with cause |
| ooxml/XWPF/Usermodel/XWPFFooter.cs:146 | E | leave | Wraps with cause |
| ooxml/XWPF/Usermodel/XWPFHeader.cs:147 | E | leave | Wraps with cause; finally closes stream |
| ooxml/XWPF/Usermodel/XWPFNumbering.cs:91 | E | leave | Wraps with cause; finally closes stream |
| ooxml/XWPF/Usermodel/XWPFSettings.cs:444 | E | leave | Wraps with cause |

### openxml4Net
| File:line | Pattern | Decision | Reason |
|---|---|---|---|
| openxml4Net/OPC/Internal/Marshallers/ZipPackagePropertiesMarshaller.cs:43 | B | log | Silently returns false; log and narrow after IOException |
| openxml4Net/OPC/PackageRelationshipCollection.cs:407 | E | rethrow | Logs but new InvalidFormatException(e.Message) drops cause |
| openxml4Net/OPC/PackagingUriHelper.cs:315 | B | narrow | ParseUri: UriFormatException/InvalidFormatException; returns null |
| openxml4Net/OPC/PackagingUriHelper.cs:362 | B | narrow | ParseUri: UriFormatException; returns null |
| openxml4Net/OPC/PackagingUriHelper.cs:421 | B | narrow | ParseUri: UriFormatException; returns null |
| openxml4Net/OPC/PackagingUriHelper.cs:680 | B | narrow | CreatePartName throws InvalidFormatException; validity probe |
| openxml4Net/OPC/ZipPackage.cs:337 | B | narrow | Degraded-mode skip; catch InvalidFormatException, log warning |
| openxml4Net/OPC/ZipPackage.cs:619 | E | leave | Specific rethrow first, wraps with cause |
| openxml4Net/Util/ZipSecureFile.cs:167 | E | log | Reflection fallback swallowed silently (log call commented out) |
