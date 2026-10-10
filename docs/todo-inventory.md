# TODO/FIXME Inventory

Triage of TODO/FIXME comments for task #38 (source issue #7). Snapshot taken on `origin/main` at a59488c6d, before this triage, across `main/`, `ooxml/`, `openxml4Net/` and `OpenXmlFormats/` (`*.cs`). Line numbers refer to that snapshot. After this triage 316 markers remain (the Character.cs TODO became a plain note).

Regenerate the raw list with:

```sh
grep -rEn --include=*.cs '(//|/\*|\*).*\b(TODO|FIXME)\b' main ooxml openxml4Net OpenXmlFormats
```

Total: 348 (main 198, ooxml 113, openxml4Net 12, OpenXmlFormats 25).

## Summary

| Category | Severity / effort | Count |
|---|---|---|
| Removed or rewritten in this task (stale / trivial) | none / trivial | 32 |
| Follow-up #106: POIFS streaming/append write | medium-high / medium-large | 17 |
| Follow-up #107: HPSF bounded reads/types | medium-high / medium-large | 6 |
| Follow-up #108: HSSF record parsing gaps | medium-high / medium-large | 20 |
| Follow-up #109: OPC zip-bomb protection | medium-high / medium-large | 8 |
| Follow-up #110: Formula eval gaps | medium-high / medium-large | 12 |
| Follow-up #111: XSSF/SXSSF gaps | medium-high / medium-large | 27 |
| Follow-up #112: XWPF gaps | medium-high / medium-large | 9 |
| Kept: behavior / correctness question (low-medium, upstream POI parity) | low-medium / varies | 137 |
| Kept: design / refactor / performance note (low) | low / varies | 48 |
| Kept: missing-test note (low) | low / small | 8 |
| Kept: documentation gap (trivial) | trivial / small | 1 |
| Kept: inside commented-out code (dead-code cleanup out of scope) | none / n.a. | 23 |

Follow-up tasks are grouped by area rather than one task per comment. Kept items are mostly inherited Apache POI notes; they stay in place until someone works in that area.

## Removed or rewritten in this task (stale / trivial)

| Location | Comment |
|---|---|
| `main/HPSF/Variant.cs:337` | * FIXME (3): Document this! |
| `main/HPSF/Variant.cs:342` | * FIXME (3): Document this! |
| `main/HPSF/Variant.cs:347` | * FIXME (3): Document this! |
| `main/HPSF/Variant.cs:352` | * FIXME (3): Document this! |
| `main/HSSF/Record/Common/UnicodeString.cs:36` | // TODO - make this when the compatibility version is Removed |
| `main/HSSF/Record/FeatHdrRecord.cs:100` | // TODO ... |
| `main/HSSF/Record/FeatRecord.cs:113` | // TODO ... |
| `main/HSSF/Record/FontRecord.cs:196` | * Set the font family (TODO) |
| `main/HSSF/Record/PageBreakRecord.cs:266` | * @param main FIXME: Document this! |
| `main/POIFS/Crypt/Standard/StandardEncryptor.cs:189` | // TODO: any properties??? |
| `main/POIFS/Crypt/Standard/StandardEncryptor.cs:232` | // TODO: any properties??? |
| `main/SS/Formula/Constant/ConstantValueParser.cs:37` | private const int TYPE_ERROR_CODE = 16; // TODO - update OOO document to include this value |
| `main/SS/Formula/Eval/Forked/ForkedEvaluator.cs:60` | // TODO: check if this is Java 9 compatible ... (FIXED task 110: type-name typo + assembly-qualified lookup) |
| `main/SS/Formula/FormulaParser.cs:1075` | //TODO Livshen's code |
| `main/SS/Formula/FormulaParser.cs:1080` | //TODO End of Livshen's code |
| `main/SS/Formula/Functions/AggregateFunction.cs:305` | else if(n == N) //TODO: Double.compare(n, N) == 0, DOSE THE "==" operator equals Double.compare |
| `main/SS/Formula/Functions/Numeric/Log.cs:61` | if(base1 == Math.E)  // TODO:  Double.compare(base, Math.E) == 0, , DOSE THE "==" operator equals Double.compare |
| `main/SS/Util/CellReference.cs:292` | // TODO |
| `main/Util/Character.cs:22` | //TODO: this should work but maybe not. |
| `ooxml/SS/Converter/ExcelToHtmlConverter.cs:120` | //TODO: HSSFWorkbook workbook = ExcelToHtmlUtils.loadXls(xlsFile); |
| `ooxml/XSSF/UserModel/XSSFVMLDrawing.cs:117` | //Stream vmlsm = new EvilUnclosedBRFixingInputStream(is1); --TODO:: add later |
| `ooxml/XSSF/UserModel/XSSFWorkbook.cs:126` | * TODO |
| `ooxml/XWPF/Usermodel/BodyElementType.cs:27` | * // TODO insert Javadoc here! |
| `OpenXmlFormats/Spreadsheet/SharedWorkbookRevisions.cs:471` | // TODO is the following correct? |
| `OpenXmlFormats/Spreadsheet/Sheet.cs:1561` | // TODO is the following correct? |
| `OpenXmlFormats/Spreadsheet/Sheet.cs:4180` | // TODO is the following correct? |
| `OpenXmlFormats/Spreadsheet/Sheet.cs:7800` | // TODO is the following correct? |
| `OpenXmlFormats/Spreadsheet/Sheet.cs:8531` | // TODO is the following correct? |
| `OpenXmlFormats/Spreadsheet/Sheet.cs:8910` | // TODO is the following correct? |
| `OpenXmlFormats/Spreadsheet/Sheet.cs:9309` | // TODO is the following correct? |
| `OpenXmlFormats/Spreadsheet/Sheet.cs:10358` | // TODO is the following correct? |
| `OpenXmlFormats/Spreadsheet/Workbook.cs:1218` | // TODO is the following correct? |

## Follow-up #106: POIFS streaming/append write

| Location | Comment |
|---|---|
| `main/POIDocument.cs:293` | * TODO Implement in-place update |
| `main/POIDocument.cs:297` | * TODO throws exception if open from stream not file |
| `main/POIFS/FileSystem/DirectoryNode.cs:235` | // TODO Work out how to report this, given we can't change the method signature... |
| `main/POIFS/FileSystem/DirectoryNode.cs:252` | /// TODO: Temporary workaround during #56791 |
| `main/POIFS/FileSystem/NDocumentInputStream.cs:250` | // TODO Do this better |
| `main/POIFS/FileSystem/NPOIFSDocument.cs:99` | //// TODO Replace with a buffer up to the mini stream size, then streaming write |
| `main/POIFS/FileSystem/NPOIFSFileSystem.cs:241` | // TODO Decide if we can handle these better whilst |
| `main/POIFS/FileSystem/NPOIFSMiniStore.cs:131` | // TODO Replace this with proper append support |
| `main/POIFS/FileSystem/NPOIFSStream.cs:42` | * TODO Implement a streaming write method, and append |
| `main/POIFS/FileSystem/NPOIFSStream.cs:140` | // TODO Streaming write support |
| `main/POIFS/FileSystem/NPOIFSStream.cs:141` | // TODO  then convert fixed sized write to use streaming internally |
| `main/POIFS/FileSystem/NPOIFSStream.cs:142` | // TODO Append write support (probably streaming) |
| `main/POIFS/FileSystem/POIFSFileSystem.cs:48` | NPOIFSFileSystem // TODO Temporary workaround during #56791 |
| `main/POIFS/FileSystem/POIFSFileSystem.cs:138` | // TODO Make this nicer! |
| `main/POIFS/Macros/VBAMacroReader.cs:266` | // TODO Refactor this to fetch dir then do the rest |
| `main/POIFS/NIO/FileBackedDataSource.cs:29` | /// TODO - Return the ByteBuffers in such a way that in RW mode, |
| `main/POIFS/NIO/FileBackedDataSource.cs:215` | //TODO: try add clean method for ByteBuffer class. |

## Follow-up #107: HPSF bounded reads/types

| Location | Comment |
|---|---|
| `main/HPSF/CodePageString.cs:55` | // TODO Some files, such as TestVisioWithCodepage.vsd, are currently |
| `main/HPSF/PropertySet.cs:420` | /* FIXME (3): Ensure that at most "length" bytes are read. */ |
| `main/HPSF/PropertySet.cs:466` | /* FIXME (3): Ensure that at most "length" bytes are read. */ |
| `main/HPSF/SummaryInformation.cs:438` | set => FirstSection.SetProperty(PropertyIDMap.PID_THUMBNAIL, /* FIXME: */ Variant.VT_LPSTR, value); |
| `main/HPSF/TypeWriter.cs:52` | LittleEndian.PutShort(out1, n); // FIXME: unsigned |
| `main/HPSF/VariantSupport.cs:31` | /// <strong>FIXME (3):</strong> Reading and writing should be made more |

## Follow-up #108: HSSF record parsing gaps

| Location | Comment |
|---|---|
| `main/HSSF/Extractor/EventBasedExcelExtractor.cs:263` | // TODO: Find object to match nrec.GetShapeId() |
| `main/HSSF/Extractor/OldExcelExtractor.cs:243` | // TODO track the XFs and Format Strings |
| `main/HSSF/Extractor/OldExcelExtractor.cs:328` | // TODO Need to fetch / use format strings |
| `main/HSSF/Record/CFRule12Record.cs:62` | // TODO Parse this, see #58150 |
| `main/HSSF/Record/CFRule12Record.cs:369` | // TODO Update ext_formatting_length |
| `main/HSSF/Record/CFRuleBase.cs:129` | protected short formatting_not_used; // TODO Decode this properly |
| `main/HSSF/Record/CFRuleBase.cs:544` | * TODO - parse conditional format formulas properly i.e. produce tRefN and tAreaN instead of tRef and tArea |
| `main/HSSF/Record/Common/FeatSmartTag.cs:37` | // TODO - process |
| `main/HSSF/Record/Crypto/Biff8DecryptingStream.cs:196` | // TODO - find out about chart BOFs |
| `main/HSSF/Record/Crypto/Biff8RC4.cs:93` | * TODO: Additionally, the lbPlyPos (position_of_BOF) field of the BoundSheet8 record MUST NOT be encrypted. |
| `main/HSSF/Record/Crypto/Biff8RC4.cs:103` | // TODO - find out about chart BOFs |
| `main/HSSF/Record/EmbeddedObjectRefSubRecord.cs:427` | return this; // TODO proper clone |
| `main/HSSF/Record/HyperlinkRecord.cs:180` | * TODO: make sense of the remaining bytes |
| `main/HSSF/Record/ObjRecord.cs:53` | // TODO - ensure 2 sub-records (ftCmo 15h, and ftEnd 00h) are always created |
| `main/HSSF/Record/ObjRecord.cs:65` | // TODO - problems with OBJ sub-records stream |
| `main/HSSF/Record/SelectionRecord.cs:32` | * TODO :  Fully implement reference subrecords. |
| `main/HSSF/Record/UnknownRecord.cs:307` | // TODO Look up more of these in the latest [MS-XLS] doc and move to getBiffName |
| `main/HSSF/UserModel/HSSFSheetConditionalFormatting.cs:52` | /// TODO - formulas containing cell references are currently not Parsed properly |
| `main/HSSF/UserModel/HSSFSheetConditionalFormatting.cs:79` | /// TODO - formulas containing cell references are currently not Parsed properly |
| `ooxml/XSSF/UserModel/XSSFSheetConditionalFormatting.cs:52` | * TODO - formulas Containing cell references are currently not Parsed properly |

## Follow-up #109: OPC zip-bomb protection

| Location | Comment |
|---|---|
| `openxml4Net/OPC/Internal/Unmarshallers/PackagePropertiesUnmarshaller.cs:63` | // TODO Load element with XMLBeans or dynamic table |
| `openxml4Net/OPC/Internal/Unmarshallers/PackagePropertiesUnmarshaller.cs:64` | // TODO Check every element/namespace for compliance |
| `openxml4Net/OPC/Internal/ZipHelper.cs:191` | // TODO: ZipSecureFile |
| `openxml4Net/OPC/Internal/ZipHelper.cs:212` | // TODO: ZipSecureFile |
| `openxml4Net/OPC/Internal/ZipHelper.cs:246` | // TODO: ZipSecureFile |
| `openxml4Net/OPC/ZipPackage.cs:62` | // TODO: ZipSecureFile |
| `openxml4Net/OPC/ZipPackage.cs:116` | // TODO: ZipSecureFile |
| `openxml4Net/OPC/ZipPackage.cs:374` | // TODO - don't use system.err.  Is it valid to return null when this exception occurs? |

## Follow-up #110: Formula eval gaps

Resolved by task 150 except D-function computed (formula) criteria, split to task #158:

| Location | Comment |
|---|---|
| `main/SS/Formula/Functions/DGet.cs:26` | * TODO: - functions as conditions (task #158) |
| `main/SS/Formula/Functions/DMax.cs:38` | /// TODO: - functions as conditions (task #158) |
| `main/SS/Formula/Functions/DMin.cs:26` | * TODO: - functions as conditions (task #158) |
| `main/SS/Formula/Functions/DStarRunner.cs:302` | // TODO: Check whether the condition cell contains a formula and return #VALUE! if it doesn't. (task #158) |
| `main/SS/Formula/Functions/DSum.cs:38` | /// TODO: - functions as conditions (task #158) |

## Follow-up #111: XSSF/SXSSF gaps

| Location | Comment |
|---|---|
| `ooxml/XSSF/Binary/XSSFBCellHeader.cs:51` | //TODO: range checking |
| `ooxml/XSSF/Binary/XSSFBCellHeader.cs:52` | bool showPhonetic = false;//TODO: fill this out |
| `ooxml/XSSF/Binary/XSSFBCellRange.cs:35` | //TODO: Convert this to generate an AreaReference |
| `ooxml/XSSF/Binary/XSSFBRecordType.cs:85` | //TODO -- implement these as needed |
| `ooxml/XSSF/Binary/XSSFBRichStr.cs:39` | //TODO: parse phonetic strings. |
| `ooxml/XSSF/Binary/XSSFBSheetHandler.cs:44` | private  bool _formulasNotResults;//TODO: implement this |
| `ooxml/XSSF/Binary/XSSFBSheetHandler.cs:89` | case XSSFBRecordType.BrtCellSt: //TODO: needs test |
| `ooxml/XSSF/Binary/XSSFBSheetHandler.cs:116` | //TODO: All the PCDI and PCDIA |
| `ooxml/XSSF/Binary/XSSFBSheetHandler.cs:172` | //TODO, read byte to figure out the type of error |
| `ooxml/XSSF/Binary/XSSFBSheetHandler.cs:179` | //TODO, read byte to figure out the type of error |
| `ooxml/XSSF/Binary/XSSFBUtils.cs:121` | //TODO: Move to LittleEndian? |
| `ooxml/XSSF/EventUserModel/XSSFSheetXMLHandler.cs:236` | // TODO Save it somewhere |
| `ooxml/XSSF/EventUserModel/XSSFSheetXMLHandler.cs:242` | // TODO Retrieve the shared formula and tweak it to |
| `ooxml/XSSF/Streaming/AutoSizeColumnTracker.cs:326` | // FIXME: if cell belongs to a merged region, some of the merged region may have fallen outside of the random access window |
| `ooxml/XSSF/Streaming/AutoSizeColumnTracker.cs:329` | // FIXME: Most cells are not merged, so calling getCellWidth twice re-computes the same value twice. |
| `ooxml/XSSF/Streaming/AutoSizeColumnTracker.cs:346` | // FIXME: if cell belongs to a merged region, some of the merged region may have fallen outside of the random access window |
| `ooxml/XSSF/Streaming/AutoSizeColumnTracker.cs:349` | // FIXME: Most cells are not merged, so calling getCellWidth twice re-computes the same value twice. |
| `ooxml/XSSF/Streaming/SXSSFCell.cs:487` | //TODO: implement correctly |
| `ooxml/XSSF/Streaming/SXSSFCreationHelper.cs:68` | //TODO: missing methods CreateExtendedColor() |
| `ooxml/XSSF/Streaming/SXSSFSheet.cs:833` | //TODO: test |
| `ooxml/XSSF/Streaming/SXSSFWorkbook.cs:1035` | //TODO: missing method isDate1904, isHidden, setHidden |
| `ooxml/XSSF/UserModel/BaseXSSFEvaluationWorkbook.cs:386` | * TODO: data tables are stored at the workbook level in XSSF, but are bound to a single sheet. |
| `ooxml/XSSF/UserModel/BaseXSSFEvaluationWorkbook.cs:391` | * FIXME: Caching tables by name here for fast formula lookup means the map is out of date if |
| `ooxml/XSSF/UserModel/BaseXSSFEvaluationWorkbook.cs:403` | // FIXME: use org.apache.commons.collections.map.CaseInsensitiveMap |
| `ooxml/XSSF/UserModel/XSSFDataBarFormatting.cs:64` | // TODO How does XSSF encode this? |
| `ooxml/XSSF/UserModel/XSSFDataBarFormatting.cs:77` | // TODO How does XSSF encode this? |
| `ooxml/XSSF/UserModel/XSSFDataBarFormatting.cs:89` | // TODO How does XSSF encode this? |

## Follow-up #112: XWPF gaps

| Location | Comment |
|---|---|
| `main/WP/UserModel/IParagraph.cs:33` | // TODO Implement justifaction in XWPF |
| `ooxml/XWPF/Usermodel/XWPFComments.cs:175` | // TODO add support for TargetMode.EXTERNAL relations. |
| `ooxml/XWPF/Usermodel/XWPFDocument.cs:613` | // TODO Add support for Even/Odd headings and footers |
| `ooxml/XWPF/Usermodel/XWPFDocument.cs:639` | // TODO Add support for Even/Odd headings and footers |
| `ooxml/XWPF/Usermodel/XWPFDocument.cs:1708` | // TODO add support for TargetMode.EXTERNAL relations. |
| `ooxml/XWPF/Usermodel/XWPFHeaderFooter.cs:325` | // TODO add support for TargetMode.EXTERNAL relations. |
| `ooxml/XWPF/Usermodel/XWPFRad.cs:26` | //TODO: Implement public radPr |
| `ooxml/XWPF/Usermodel/XWPFRun.cs:1287` | // TODO |
| `ooxml/XWPF/Usermodel/XWPFTableRow.cs:205` | //TODO: it is possible to have an SDT that contains a cell in within a row |

## Kept: behavior / correctness question (low-medium, upstream POI parity)

| Location | Comment |
|---|---|
| `main/DDF/EscherArrayProperty.cs:201` | // TODO: this part is strange - it doesn't make sense to compare |
| `main/DDF/EscherContainerRecord.cs:289` | // TODO - keep looping? Do we expect multiple matches? |
| `main/GlobalSuppressions.cs:12` | // TODO fix warnings: |
| `main/GlobalSuppressions.cs:42` | // TODO fix warnings: |
| `main/GlobalSuppressions.cs:78` | // TODO: apply fix that is proposed in the code: |
| `main/GlobalSuppressions.cs:91` | // TODO: fix needs some minutes, because the constructor adds itself to the given parameter BlockList. |
| `main/HSSF/Model/InternalSheet.cs:55` | protected int dimsloc = -1;  // TODO - Is it legal for dims record to be missing? |
| `main/HSSF/Model/InternalSheet.cs:2164` | // TODO - adjust data validations |
| `main/HSSF/Model/InternalWorkbook.cs:2293` | // TODO - what does '-1' mean here? |
| `main/HSSF/Model/LinkTable.cs:314` | * TODO - would not be required if calling code used RecordStream or similar |
| `main/HSSF/Record/Aggregates/Chart/FrameAggregate.cs:64` | //TODO: write StartBlockRecord |
| `main/HSSF/Record/CRNCountRecord.cs:51` | // TODO - seems like the sign bit of this field might be used for some other purpose |
| `main/HSSF/Record/EmbeddedObjectRefSubRecord.cs:49` | // TODO: Consider making a utility class for these.  I've discovered the same field ordering |
| `main/HSSF/Record/ExternalNameRecord.cs:270` | // TODO - determine exact conditions when formula Is present |
| `main/HSSF/Record/NameRecord.cs:558` | return new Area3DPtg("A1:A1", 0); // TODO - change to not be partially initialised |
| `main/HSSF/UserModel/HSSFCell.cs:986` | /// TODO - perhaps a method like SetCellTypeAndValue(int, Object) should be introduced to avoid this |
| `main/HSSF/UserModel/HSSFDataValidation.cs:57` | //FIXME: This cast can be avoided. |
| `main/HSSF/UserModel/HSSFEvaluationWorkbook.cs:145` | // TODO Update this to expose first and last sheet indexes |
| `main/HSSF/UserModel/HSSFEvaluationWorkbook.cs:284` | return _nameRecord.HasFormula; // TODO - is this right? |
| `main/HSSF/UserModel/HSSFPatriarch.cs:445` | /// FIXME - detect chart in all cases (only seems |
| `main/HSSF/UserModel/HSSFPatriarch.cs:453` | // TODO - support charts properly in usermodel |
| `main/HSSF/UserModel/HSSFPicture.cs:276` | // TODO: add trailing \u0000? |
| `main/HSSF/UserModel/HSSFRow.cs:271` | // TODO - RowRecord column boundaries need to be updated for cell comments too |
| `main/HSSF/UserModel/HSSFSheet.cs:1485` | /// TODO: MODE , this is only row specific |
| `main/HSSF/UserModel/HSSFSheet.cs:1519` | /// TODO Might want to add bounds checking here |
| `main/HSSF/UserModel/HSSFSheet.cs:1536` | /// TODO Might want to Add bounds Checking here |
| `main/HSSF/UserModel/HSSFSimpleShape.cs:144` | //TODO add other shape types which can not contain text |
| `main/HSSF/UserModel/HSSFWorkbook.cs:881` | // TODO - maybe same logic required for other/all built-in name records |
| `main/HSSF/UserModel/HSSFWorkbook.cs:1518` | // TODO - Add similar sanity Check to Ensure that Sheet.SerializeIndexRecord() does not Write mis-aligned offsets either |
| `main/HSSF/UserModel/StaticFontMetrics.cs:49` | /* TODO - SixLabors.Fonts: |
| `main/POIFS/Crypt/CryptoFunctions.cs:583` | // TODO: charset conversion (see ecma spec) |
| `main/POIFS/Crypt/Standard/StandardDecryptor.cs:57` | // TODO: check and Trim/pad the hashes to 32 |
| `main/POIFS/Storage/RawDataBlock.cs:127` | // TODO return null instead of raising an unexpected exception (CA1065) |
| `main/SS/Format/CellFormat.cs:68` | * TODO Re-use parts of this logic with {@link ConditionalFormatting} / |
| `main/SS/Format/CellFormat.cs:70` | * TODO Support the full set of modifiers, including alternate calendars and |
| `main/SS/Format/CellNumberFormatter.cs:234` | // TODO: if decimalPoint is null (-> index == -1), return the whole list? |
| `main/SS/Formula/ConditionalFormattingEvaluator.cs:213` | /// TODO: eventually this should work like <see cref="EvaluationCache.notifyUpdateCell(int, int, EvaluationCell)" /> |
| `main/SS/Formula/Constant/ConstantValueParser.cs:42` | // TODO - is this the best way to represent 'EMPTY'? |
| `main/SS/Formula/Eval/BoolEval.cs:47` | // TODO - Find / Replace all occurrences |
| `main/SS/Formula/EvaluationTracker.cs:36` | // TODO - consider deleting this class and letting CellEvaluationFrame take care of itself |
| `main/SS/Formula/FormulaParser.cs:1101` | // TODO - what about NameX ? |
| `main/SS/Formula/FormulaParser.cs:1210` | int ptr = _pointer - 1; // TODO avoid StringIndexOutOfBounds |
| `main/SS/Formula/FormulaRenderer.cs:54` | // TODO - what about MemNoMemPtg? |
| `main/SS/Formula/FormulaRenderer.cs:59` | // TODO - Put comment and throw exception in ToFormulaString() of these classes |
| `main/SS/Formula/Function/FunctionMetadataReader.cs:105` | // TODO - make POI use IsVolatile |
| `main/SS/Formula/Functions/Count.cs:34` | * TODO: Check this properly Matches excel on edge cases |
| `main/SS/Formula/Functions/Match.cs:257` | // TODO - Is binary search used for (match_type==+1) ? |
| `main/SS/Formula/Functions/MultiOperandNumericFunction.cs:267` | // TODO this doesn't seem right.  Fix or Add comment. |
| `main/SS/Formula/Functions/Sumproduct.cs:241` | // TODO - shouldn't BlankEval.INSTANCE be used always instead of null? |
| `main/SS/Formula/Functions/Value.cs:55` | * TODO see if the same functionality is needed in {@link OperandResolver#parseDouble(String)} |
| `main/SS/Formula/LazyRefEval.cs:31` | * TODO Provide access to multiple sheets where present |
| `main/SS/Formula/OperandClassTransformer.cs:157` | // TODO is any Token transformation required under the various ref operators? |
| `main/SS/Formula/OperationEvaluationContext.cs:244` | // ugly typecast - TODO - make spReadsheet version more easily accessible |
| `main/SS/Formula/PTG/AreaPtgBase.cs:36` | * TODO - (May-2008) fix subclasses of AreaPtg 'AreaN~' which are used in shared formulas. |
| `main/SS/Formula/PTG/ArrayPtg.cs:232` | return ""; // TODO - how is 'empty value' represented in formulas? |
| `main/SS/Formula/PTG/MemAreaPtg.cs:76` | return ""; // TODO: Not sure how to format this. -- DN |
| `main/SS/Formula/PTG/PowerPtg.cs:47` | // TODO - 2 seems wrong (Jun 2008).  Maybe this method is not relevant |
| `main/SS/Formula/PTG/Ptg.cs:166` | return new UnknownPtg(); // TODO - not a real Ptg |
| `main/SS/UserModel/AutoFilter.cs:28` | * TODO YK: For now (Aug 2010) POI only supports Setting a basic autofilter on a range of cells. |
| `main/SS/UserModel/DataFormatter.cs:516` | // TODO - when does this occur? |
| `main/SS/UserModel/IgnoredErrorType.cs:25` | * TODO Implement these for HSSF too, using FeatFormulaErr2, |
| `main/SS/Util/AreaReference.cs:61` | // TODO - probably shouldn't initialize area ref when text is really a cell ref |
| `main/SS/Util/AreaReference.cs:93` | // TODO - whole row refs |
| `main/SS/Util/AreaReference.cs:494` | // TODO - are references like "Sheet1!A1:Sheet1:B2" ever valid? |
| `main/SS/Util/CellAddress.cs:81` | // FIXME: breaks if Address Contains a sheet name or dollar signs from an absolute CellReference |
| `main/SS/Util/CellReference.cs:72` | // FIXME: _sheetName may be null, depending on the entry point. |
| `main/SS/Util/CellReference.cs:141` | // TODO - "-1" is a special value being temporarily used for whole row and whole column area references. |
| `main/SS/Util/CellUtil.cs:127` | //TODO:shift cells |
| `main/SS/Util/CellUtil.cs:143` | // FIXME: Cached value may be stale |
| `main/SS/Util/CellUtil.cs:240` | //TODO:shift cells |
| `main/SS/Util/MutableFPNumber.cs:26` | // TODO - what about values between (10<sup>14</sup>-0.5) and (10<sup>14</sup>-0.05) ? |
| `main/SS/Util/SheetUtil.cs:612` | // TODO: support rich text fragments |
| `main/Util/BigInteger.cs:3206` | //TODO: to complete this method,we should implement SignedMutableBigInteger class |
| `main/WP/UserModel/ICharacterRun.cs:79` | // TODO Review these, and add to XWPFRun if possible |
| `main/WP/UserModel/IParagraph.cs:37` | // TODO Expose the different page break related things, |
| `ooxml/POIFS/Crypt/Agile/AgileDecryptor.cs:342` | // TODO: calculate integrity hmac while Reading the stream |
| `ooxml/POIFS/Crypt/Agile/AgileEncryptionVerifier.cs:130` | SpinCount = (100000); // TODO: use parameter |
| `ooxml/POIFS/Crypt/Agile/AgileEncryptor.cs:179` | // TODO: Initialize headers |
| `ooxml/POIFS/Crypt/Agile/AgileEncryptor.cs:196` | // TODO: add stream size parameter to GetDataStream() |
| `ooxml/XDDF/UserModel/Chart/XDDFChart.cs:495` | // TODO repeat above code for all kind of charts |
| `ooxml/XDDF/UserModel/Chart/XDDFPieChartData.cs:81` | //TODO: Is this casting(generic type Upcasting) right? |
| `ooxml/XSSF/EventUserModel/XSSFSheetXMLHandler.cs:387` | // TODO: Can these ever have formatting on them? |
| `ooxml/XSSF/Extractor/EmbeddedExtractor.cs:213` | // TODO: inspect the CompObj record for more details, i.e. the content type |
| `ooxml/XSSF/Extractor/EmbeddedExtractor.cs:268` | // TODO: investigate if this is just an EMF-hack or if other formats are also embedded in EMF |
| `ooxml/XSSF/Extractor/EmbeddedExtractor.cs:410` | // TODO: read the content type from CombObj stream |
| `ooxml/XSSF/Extractor/XSSFExportToXml.cs:210` | // TODO:  implement filtering management in xpath |
| `ooxml/XSSF/Model/ExternalLinksTable.cs:170` | // TODO Last seen data |
| `ooxml/XSSF/Streaming/SXSSFDrawing.cs:29` | /// TODO: Potentially, Comment and Chart need a similar streaming wrapper like Picture. |
| `ooxml/XSSF/Streaming/SXSSFSheet.cs:1371` | //TODO: review this. |
| `ooxml/XSSF/UserModel/BaseXSSFEvaluationWorkbook.cs:211` | // TODO Return a more specialised form of this, see bug #56752 |
| `ooxml/XSSF/UserModel/BaseXSSFEvaluationWorkbook.cs:224` | return new ExternalName(nameName, nameIdx, 0);  // TODO Is this right? |
| `ooxml/XSSF/UserModel/BaseXSSFEvaluationWorkbook.cs:476` | // TODO - no idea if this is right |
| `ooxml/XSSF/UserModel/BaseXSSFEvaluationWorkbook.cs:496` | return HasFormula; // TODO - is this right? |
| `ooxml/XSSF/UserModel/Charts/XSSFNumberCache.cs:101` | /* TODO: consider more effective algorithm. Left as is since |
| `ooxml/XSSF/UserModel/Extensions/XSSFCellBorder.cs:222` | //TODO: change the compare logic |
| `ooxml/XSSF/UserModel/Helpers/XSSFPasswordHelper.cs:170` | // TODO: is "velvetSweatshop" the default password? |
| `ooxml/XSSF/UserModel/XSSFCell.cs:1335` | * TODO - perhaps a method like SetCellTypeAndValue(int, Object) should be introduced to avoid this |
| `ooxml/XSSF/UserModel/XSSFCellStyle.cs:89` | // TODO decide on a style ctxf |
| `ooxml/XSSF/UserModel/XSSFColor.cs:417` | // FIXME: this method would be more useful if it could convert any Color to an XSSFColor |
| `ooxml/XSSF/UserModel/XSSFDataValidationConstraint.cs:99` | //FIXME: Need to confirm if this is not a formula. |
| `ooxml/XSSF/UserModel/XSSFDialogsheet.cs:25` | //YK: TODO: this is only a prototype |
| `ooxml/XSSF/UserModel/XSSFDrawing.cs:625` | // TODO: handle vflip/hflip |
| `ooxml/XSSF/UserModel/XSSFHyperlink.cs:116` | //FIXME: change to protected if/when SXSSFHyperlink class is created |
| `ooxml/XSSF/UserModel/XSSFRelation.cs:267` | null,//TODO: figure out what this should be? |
| `ooxml/XSSF/UserModel/XSSFRelation.cs:274` | null,//TODO: figure out what this should be? |
| `ooxml/XSSF/UserModel/XSSFRelation.cs:281` | null,//TODO: figure out what this should be? |
| `ooxml/XSSF/UserModel/XSSFRow.cs:524` | // FIXME: is this something that rowShifter could be doing? |
| `ooxml/XSSF/UserModel/XSSFShapeGroup.cs:250` | // TODO: calculate bounding rectangle on anchor and set off/ext correctly |
| `ooxml/XSSF/UserModel/XSSFSheet.cs:2857` | //TODO: The following codes are not same as poi, re-do it?. |
| `ooxml/XSSF/UserModel/XSSFSheet.cs:3268` | // FIXME: is special behavior needed if srcRows and destRows belong to the same sheets and the regions overlap? |
| `ooxml/XSSF/UserModel/XSSFSheet.cs:3277` | // FIXME: if srcRows contains gaps or null values, clear out those rows that will be overwritten |
| `ooxml/XSSF/UserModel/XSSFSheet.cs:3305` | // FIXME: is this something that rowShifter could be doing? |
| `ooxml/XSSF/UserModel/XSSFWorkbook.cs:902` | //TODO: this is extra somehow |
| `ooxml/XSSF/UserModel/XSSFWorkbook.cs:2643` | // TODO: generate CombObj stream |
| `ooxml/XWPF/Model/XWPFHeaderFooterPolicy.cs:440` | * TODO: manage all the other variables |
| `ooxml/XWPF/Model/XWPFHeaderFooterPolicy.cs:456` | // TODO generate rsidr and rsidrdefault |
| `ooxml/XWPF/Usermodel/XWPFDocument.cs:601` | // TODO this needs to be migrated out into section code |
| `ooxml/XWPF/Usermodel/XWPFDocument.cs:627` | // TODO this needs to be migrated out into section code |
| `ooxml/XWPF/Usermodel/XWPFDocument.cs:1201` | /* TODO update body element, update xwpf element, verify that |
| `ooxml/XWPF/Usermodel/XWPFNum.cs:23` | * TODO Bring more of the logic over from XWPFParagraph |
| `ooxml/XWPF/Usermodel/XWPFParagraph.cs:1000` | // TODO Fix this to convert line to equivalent value, or deprecate this in |
| `ooxml/XWPF/Usermodel/XWPFStyles.cs:341` | * TODO Replace this with specific Setters for each type, possibly |
| `openxml4Net/OPC/OPCPackage.cs:122` | // TODO Delocalize specialized marshallers |
| `openxml4Net/OPC/PackagePart.cs:88` | this._container = (ZipPackage) pack; // TODO - enforcing ZipPackage here - perhaps should change constructor signature |
| `openxml4Net/OPC/PackagingUriHelper.cs:747` | * TODO YK: for now this method does only (5). Finish the rest. |
| `openxml4Net/Util/XmlHelper.cs:250` | //TODO make this stable. |
| `OpenXmlFormats/Drawing/Chart/Chart.cs:1338` | //TODO: parse CT_LegendEntry |
| `OpenXmlFormats/Drawing/ShapeEffects.cs:50` | //TODO: implement http://www.schemacentral.com/sc/ooxml/t-a_CT_Blip.html |
| `OpenXmlFormats/Spreadsheet/PivotTable/CT_PivotCacheDefinition.cs:144` | //TODO add namespaceUri |
| `OpenXmlFormats/Spreadsheet/PivotTable/CT_PivotCacheRecords.cs:64` | //TODO add namespaceUri |
| `OpenXmlFormats/Spreadsheet/SharedString/CT_Rst.cs:42` | //TODO: diff has-space case and no-space case |
| `OpenXmlFormats/Spreadsheet/Sheet/CT_Comment.cs:96` | [XmlAttribute("guid")] // 0..1 TODO: Type is ST_Guid |
| `OpenXmlFormats/Spreadsheet/Sheet.cs:4245` | // these are optional attributes except guid - TODO make them optional |
| `OpenXmlFormats/Spreadsheet/Sheet.cs:8422` | // TODO: Parse CT_ObjectPr xml node |
| `OpenXmlFormats/Spreadsheet/Styles/CT_Colors.cs:406` | res.themeField = this.themeField; // TODO change all the uses theme to use uint instead of signed integer variants |
| `OpenXmlFormats/Wordprocessing/wml.cs:736` | // TODO is the following correct/better with regard the namespace? |
| `OpenXmlFormats/Wordprocessing/wml.cs:1291` | // TODO is the following correct/better with regard the namespace? |

## Kept: design / refactor / performance note (low)

| Location | Comment |
|---|---|
| `main/HSSF/EventUserModel/FormatTrackingHSSFListener.cs:95` | * TODO - move this to a central class in such a |
| `main/HSSF/Model/InternalChart.cs:878` | r.Pattern = ((short) 1);			 // TODO: Add Pattern constants to record |
| `main/HSSF/Model/InternalSheet.cs:211` | // TODO - take chart streams off into separate java objects |
| `main/HSSF/Model/InternalWorkbook.cs:459` | // TODO - do we need "this.records.Remove(...);" similar to that in this.RemoveName(int namenum) {}? |
| `main/HSSF/Model/LinkTable.cs:63` | // TODO make this class into a record aggregate |
| `main/HSSF/Model/LinkTable.cs:223` | private readonly WorkbookRecordList _workbookRecordList; // TODO - would be nice to Remove this |
| `main/HSSF/Model/LinkTable.cs:345` | // TODO - do we need "Workbook.records.Remove(...);" similar to that in Workbook.RemoveName(int namenum) {}? |
| `main/HSSF/Model/LinkTable.cs:437` | // TODO - this Is messy |
| `main/HSSF/Model/RecordOrderer.cs:36` | // TODO - simplify logic using a generalised record ordering |
| `main/HSSF/Record/Aggregates/RowRecordsAggregate.cs:189` | //TODO: correct it, SeriesIndexRecord will appear in a separate chart sheet that contains a single chart |
| `main/HSSF/Record/DBCellRecord.cs:172` | // TODO - make immutable. |
| `main/HSSF/Record/EmbeddedObjectRefSubRecord.cs:45` | private int field_1_unknown_int;                            // Unknown stuff at the front.  TODO: Confirm that it's a short[] |
| `main/HSSF/Record/HyperlinkRecord.cs:468` | out1.WriteInt(0x00000002); // TODO const |
| `main/HSSF/Record/HyperlinkRecord.cs:520` | out1.WriteShort(0x0003); // TODO const |
| `main/HSSF/Record/StyleRecord.cs:45` | private int field_1_xf_index;   // TODO: bitfield candidate |
| `main/HSSF/UserModel/HSSFChart.cs:887` | r.Pattern = ((short) 1);			 // TODO: Add Pattern constants to record |
| `main/HSSF/UserModel/HSSFRow.cs:65` | // TODO - ditch this constructor |
| `main/HSSF/UserModel/HSSFSheet.cs:737` | // FIXME: this may be faster if it looped over array formulas directly rather than looping over each cell in |
| `main/HSSF/UserModel/HSSFWorkbook.cs:2157` | // TODO: Some kind of structure. |
| `main/HSSF/Util/HSSFColor.cs:45` | // TODO make subclass instances immutable |
| `main/SS/Format/CellFormat.cs:66` | * TODO Merge this with {@link DataFormatter} so we only have one set of |
| `main/SS/Formula/Eval/OperandResolver.cs:347` | // TODO - remove 've == null' condition once AreaEval is fixed |
| `main/SS/Formula/EvaluationCache.cs:182` | // TODO - if we are confident that this sanity check is not required, we can Remove 'value' from plain value cache entry |
| `main/SS/Formula/Functions/LookupUtils.cs:439` | // TODO move parseBoolean to OperandResolver |
| `main/SS/UserModel/AutoFilter.cs:58` | * TODO YK: think how to combine AutoFilter with with DataValidationConstraint, they are really close relatives |
| `main/SS/UserModel/DataFormatter.cs:336` | // TODO Going forward, we should really merge the logic between the two classes |
| `main/SS/Util/AreaReference.cs:430` | // TODO - refactor cell reference parsing logic to one place. |
| `main/SS/Util/CellReference.cs:389` | // TODO - refactor cell reference parsing logic to one place. |
| `main/Util/PushbackStream.cs:107` | // TODO Can this case be made more efficient? |
| `main/WP/UserModel/IParagraph.cs:91` | // TODO Make the HWPF and XWPF interface wrappers compatible for these |
| `ooxml/POIFS/Crypt/Dsig/Services/RelationshipTransformService.cs:177` | // TODO: remove non element nodes ??? |
| `ooxml/XSSF/Model/StylesTable.cs:706` | // TODO: check for duplicate |
| `ooxml/XSSF/Streaming/SXSSFSheet.cs:37` | // TODO: fields should be private and use public property |
| `ooxml/XSSF/UserModel/XSSFGraphicFrame.cs:51` | // TODO: there may be a better way to delegate this |
| `ooxml/XSSF/UserModel/XSSFRow.cs:477` | // FIXME: remove type casting when copyCellFrom(Cell, CellCopyPolicy) is added to Cell interface |
| `ooxml/XSSF/UserModel/XSSFSheet.cs:68` | //TODO make the two variable below private! |
| `ooxml/XSSF/UserModel/XSSFSheet.cs:4990` | // FIXME: this may be faster if it looped over array formulas |
| `ooxml/XSSF/UserModel/XSSFSheet.cs:5527` | // FIXME: (performance optimization) this should be moved outside the for-loop so that comments only needs to be iterated over once. |
| `ooxml/XSSF/UserModel/XSSFSheet.cs:5553` | // FIXME: (performance optimization) this should be moved outside the for-loop so that hyperlinks only needs to be iterated over once. |
| `ooxml/XSSF/UserModel/XSSFSimpleShape.cs:41` | { // TODO - instantiable superclass |
| `ooxml/XWPF/Usermodel/XWPFDefaultParagraphStyle.cs:25` | * TODO Share logic with {@link XWPFParagraph} which also uses CTPPr |
| `ooxml/XWPF/Usermodel/XWPFDefaultRunStyle.cs:25` | * TODO Share logic with {@link XWPFRun} which also uses CTRPr |
| `ooxml/XWPF/Usermodel/XWPFDocument.cs:236` | // TODO: make me optional/Separated in private function |
| `ooxml/XWPF/Usermodel/XWPFRun.cs:1386` | // TODO Should we have an interface for this sort of thing? |
| `OpenXmlFormats/Spreadsheet/CustomXmlMappings.cs:108` | private System.Xml.XmlElement anyField; // TODO ensure initialization = new XmlElement(); // 1..1 |
| `OpenXmlFormats/Spreadsheet/Styles/CT_Colors.cs:183` | private uint themeField; // TODO change all the uses theme to use uint instead of signed integer variants |
| `OpenXmlFormats/Spreadsheet/Styles/CT_Fill.cs:115` | //TODO: NPOI  duplication code, fix it. |
| `OpenXmlFormats/Vml/Main.cs:1340` | private bool invyFieldSpecified; // TODO remove |

## Kept: missing-test note (low)

| Location | Comment |
|---|---|
| `main/HSSF/Model/InternalSheet.cs:328` | // TODO - would like to keep the chart aggregate packed, but one unit test needs attention |
| `main/HSSF/Record/Aggregates/CFRecordsAggregate.cs:172` | { // TODO -(MAR-2008) can this ever happen? Write junit |
| `main/HSSF/Record/FileSharingRecord.cs:57` | // TODO - Current examples(3) from junits only have zero Length username. |
| `main/HSSF/Record/FileSharingRecord.cs:128` | // TODO - junit |
| `main/SS/Formula/Functions/Sumif.cs:72` | // TODO - junit to prove last arg must be srcColumnIndex and not srcRowIndex |
| `main/SS/Formula/Functions/Sumproduct.cs:161` | int width = firstArg.LastColumn - firstArg.FirstColumn + 1; // TODO - junit |
| `main/SS/Formula/Functions/ValueEvaluationHelper.cs:68` | int width = evalArg.LastColumn - evalArg.FirstColumn + 1; // TODO - junit |
| `main/SS/UserModel/BuiltinFormats.cs:109` | // TODO - one junit relies on these values which seems incorrect |

## Kept: documentation gap (trivial)

| Location | Comment |
|---|---|
| `ooxml/XWPF/Usermodel/ParagraphAlignment.cs:33` | //YK: TODO document each alignment option |

## Kept: inside commented-out code (dead-code cleanup out of scope)

| Location | Comment |
|---|---|
| `main/HSSF/Record/Aggregates/SharedValueManager.cs:230` | //    // TODO - fix file "15228.xls" so it opens in Excel after rewriting with POI |
| `main/HSSF/Record/EscherAggregate.cs:998` | //    // TODO: Support Converting our records |
| `main/HSSF/Record/EscherAggregate.cs:1060` | //            // TODO |
| `main/HSSF/Record/UnknownRecord.cs:94` | //    // TODO - put unknown OBJ sub-records in a different class |
| `main/SS/Formula/Eval/OperandResolver.cs:307` | //// TODO - support notation like '1E3' (==1000) |
| `main/SS/Formula/Formula.cs:50` | //        // TODO - this seems to occur when IntersectionPtg is present |
| `main/SS/Formula/PTG/Ptg.cs:243` | //    // TODO - all base tokens are logically immutable, but AttrPtg needs some clean-up |
| `main/SS/Util/CellRangeUtil.cs:183` | //// TODO - write junit test for this |
| `main/SS/Util/SheetUtil.cs:871` | //    TODO-Fonts: not supported: if (font.Underline == (byte)FontUnderlineType.SINGLE) str.AddAttribute(TextAttribute.UNDERLINE, TextAttr... |
| `main/Util/PushbackInputStream.cs:162` | //		// TODO Can this case be made more efficient? |
| `ooxml/POIFS/Crypt/Dsig/Facets/OOXMLSignatureFacet.cs:119` | //        // TODO: find a better way ... |
| `ooxml/POIFS/Crypt/Dsig/Facets/XAdESSignatureFacet.cs:214` | //        // TODO: check if issuerName is different on GetTBSCertificate |
| `ooxml/POIFS/Crypt/Dsig/SignatureInfo.cs:154` | //    // TODO: check for XXE |
| `ooxml/POIFS/Crypt/Dsig/SignatureInfo.cs:182` | //        // TODO: replace with property when xml-sec patch is applied |
| `ooxml/POIFS/Crypt/Dsig/SignatureInfo.cs:502` | // * TODO: we could be using DigestOutputStream here to optimize memory |
| `ooxml/POIXMLPropertiesTextExtractor.cs:177` | //    // TODO Fetch the array values and output |
| `ooxml/POIXMLPropertiesTextExtractor.cs:181` | //    // TODO Fetch the vector values and output |
| `ooxml/POIXMLPropertiesTextExtractor.cs:186` | //    // TODO Decode, if possible |
| `ooxml/POIXMLPropertiesTextExtractor.cs:191` | //    // TODO Decode, if possible |
| `ooxml/POIXMLPropertiesTextExtractor.cs:195` | //    // TODO Decode, if possible |
| `ooxml/XWPF/Usermodel/XWPFDocument.cs:797` | //     * TODO DO not use a coded constant, find the constant in the OOXML |
| `OpenXmlFormats/Spreadsheet/CustomXmlMappings.cs:20` | //  TODO the initial elements of schemaField and mapField must be ensured somewhere else - or is there a save default!? |

## Task 110 deferred (F5 formula-eval items)

Done in task 150: 1900/1904 windowing (`IEvaluationWorkbook.IsDate1904`, `IDate1904AwareFunction` for DATE, YEAR..SECOND, TEXT), R1C1 in ADDRESS and INDIRECT, DateParser culture date order, NumberEval 15-digit text (already via `NumberToTextConverter`; tests added), TEXT numeric-text coercion (dead DecimalFormat port removed; `DataFormatter` does the work), D-function wildcard and text-comparison criteria, CF top-10 ranking only numbers (plus `CT_CfRule.Set` now copies `rank`). Remaining: D-function computed criteria, task #158.
