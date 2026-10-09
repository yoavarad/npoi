# Dynamic-array formulas (task #152)

Apache POI has no spill support, so this design follows how Excel 365 stores dynamic arrays in
`.xlsx` and reuses NPOI's existing array-formula (CSE) machinery.

## Functions
`FILTER`, `SORT`, `UNIQUE`, `SEQUENCE` live in `main/SS/Formula/Atp/DynamicArrayFunctions.cs` and
are registered in `AnalysisToolPak` like XLOOKUP. Each returns a `CacheAreaEval` (or an error).
`FindFunction` strips both `_xlfn.` and `_xlws.`, so the stored names Excel writes
(`_xlfn._xlws.FILTER`, `_xlfn._xlws.SORT`, `_xlfn.UNIQUE`, `_xlfn.SEQUENCE`) and the bare names both
evaluate. Write the prefixed names if Excel must open the file: NPOI does not add them.

Two Excel error values are added to `FormulaError`/`ErrorEval`: `#SPILL!` (code 0x2D) and `#CALC!`
(0x32). The codes are Excel's VBA `xlErrSpill`/`xlErrCalc` minus 2000; `.xlsx` stores the text, so
they matter only internally.

Semantics (from the Excel documentation): FILTER keeps rows (include is a column of the array's
height) or columns (a row of its width); include values are booleans/numbers, text gives `#VALUE!`,
errors propagate; no match gives `if_empty` or `#CALC!`. SORT is stable, takes one or more
`sort_index`/`sort_order` keys (order 1 or -1) and `by_col`; ascending order is numbers, text
(case-insensitive), FALSE, TRUE, errors, with blanks last in both directions. UNIQUE compares text
case-insensitively, keeps first occurrences, and supports `by_col` and `exactly_once` (`#CALC!` when
nothing is left). SEQUENCE truncates rows/columns, gives `#CALC!` for 0 and `#VALUE!` for negatives.

## Storage: a dynamic array is an array formula with cell metadata
Excel writes a spilled formula as an ordinary array formula on the anchor
(`<f t="array" ref="A1:A3">`) whose cell carries `cm="1"`, pointing at an `XLDAPR` record in
`/xl/metadata.xml`. The spilled cells hold only cached values. NPOI already reads `t="array"` ranges
as array-formula groups and keeps `cm` on `CT_Cell`, so:

- `XSSFCell.IsDynamicArrayFormula`: the cell's array-formula anchor has `cm != 0`.
- `XSSFSheet.SetDynamicArrayFormula(formula, anchor)`: a 1x1 array formula with `cm = 1`, and the
  workbook gets a metadata part (`XSSFSheetMetadata`, relation `XSSFRelation.SHEET_METADATA`) holding
  the single XLDAPR record Excel writes. A metadata part read from a file is kept byte-for-byte.
- `.xls` has no dynamic-array storage: there the functions work inside legacy array formulas
  (`SetArrayFormula` over a range you size yourself), never spilling.

## Evaluation and spill
- `WorkbookEvaluator.EvaluateArrayResult(cell)` evaluates a formula without reducing an array result
  to one value, so its size is known.
- `XSSFFormulaEvaluator.EvaluateFormulaCell` (and so `EvaluateAll`) on a dynamic-array anchor
  computes the target range from the result's size. If every cell in it, outside the formula's own
  current range, is missing or blank, no merged region overlaps it, and it fits on the sheet, the
  array-formula range is resized to it (cells left behind are blanked) and every cell gets its cached
  value; blanks in the result become 0, as in Excel. Otherwise the range shrinks to the anchor, which
  gets `#SPILL!`. Any range change clears the evaluator cache, since other formulas may read those cells.
- `WorkbookEvaluator.DereferenceResult` returns `#SPILL!` for a cell of a dynamic array whose stored
  range does not match the result's size (`IDynamicArrayEvaluationCell`, implemented by
  `XSSFEvaluationCell`), so `Evaluate` and dependent formulas agree with the cached `#SPILL!` until the
  anchor is re-evaluated.
- `BaseFormulaEvaluator.EvaluateAllFormulaCells` takes a snapshot of the cells before evaluating,
  because spilling adds rows and cells.
- A plain `SetCellFormula` formula keeps legacy (implicit intersection) behaviour: `=SEQUENCE(3)` in
  an ordinary formula cell evaluates to 1 and does not spill, like Excel's `@SEQUENCE(3)`.

## Limitations
- Spilled cells are array-formula members, so writing a value into one does not block the spill as
  in Excel (NPOI keeps them part of the array); remove the formula with `RemoveArrayFormula` first.
- A workbook whose existing `metadata.xml` has no XLDAPR cell-metadata record (for example only
  rich-value metadata) is not rewritten: new dynamic-array formulas then point `cm="1"` at whatever
  record 1 is.
- SXSSF does not spill (streaming cannot rewrite earlier rows).
- Any non-zero `cm` on an array-formula anchor counts as dynamic (Excel only uses cell metadata for
  XLDAPR in practice). Resizing re-sets the array formula from its text, so formula attributes such as
  `ca`/`aca` are not kept. A table (ListObject) in the way does not block a spill unless its cells are
  non-empty.
- SEQUENCE results above 10,000,000 cells give `#NUM!` (memory guard; Excel's limit is the sheet).
- `EvaluateAll` repeats its pass (up to 10 times) while spill ranges keep changing, so formulas
  evaluated before an anchor see the spilled values; `Evaluate`/`EvaluateInCell` on an anchor whose
  stored range does not match give `#SPILL!` without spilling.
- Not implemented: SORTBY, RANDARRAY, LET, LAMBDA, TAKE, DROP, VSTACK, HSTACK, TOCOL, TOROW; the
  financial leftovers from #139 (ACCRINT, AMORDEGRC, AMORLINC, ODDF*/ODDL*, PRICEMAT, YIELDMAT, VDB
  fractional parity) stay open.
