using NPOI.OpenXml4Net.OPC;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.IO;
using System.Text;

namespace TestCases.XSSF.UserModel
{
    /// <summary>
    /// Spilling of dynamic-array formulas (XSSFSheet.SetDynamicArrayFormula + XSSFFormulaEvaluator).
    /// </summary>
    [TestFixture]
    public class TestXSSFDynamicArrays
    {
        private static ICell Cell(ISheet sheet, string reference)
        {
            var cr = new CellReference(reference);
            return sheet.GetRow(cr.Row)?.GetCell(cr.Col);
        }

        private static ICell CreateCell(ISheet sheet, string reference)
        {
            var cr = new CellReference(reference);
            IRow row = sheet.GetRow(cr.Row) ?? sheet.CreateRow(cr.Row);
            return row.GetCell(cr.Col) ?? row.CreateCell(cr.Col);
        }

        [Test]
        public void TestSpillsIntoNeighbouringCells()
        {
            using var wb = new XSSFWorkbook();
            var sheet = (XSSFSheet) wb.CreateSheet();
            var fe = new XSSFFormulaEvaluator(wb);
            ICell anchor = sheet.SetDynamicArrayFormula("_xlfn.SEQUENCE(3,2)", new CellReference("A1"));

            ClassicAssert.AreEqual(CellType.Numeric, fe.EvaluateFormulaCell(anchor));

            ClassicAssert.AreEqual("A1:B3", anchor.ArrayFormulaRange.FormatAsString());
            for(int i = 0; i < 6; i++)
            {
                ICell cell = sheet.GetRow(i / 2).GetCell(i % 2);
                ClassicAssert.AreEqual(CellType.Formula, cell.CellType);
                ClassicAssert.AreEqual(i + 1d, cell.NumericCellValue);
                ClassicAssert.IsTrue(((XSSFCell) cell).IsDynamicArrayFormula);
            }
            ClassicAssert.AreEqual(6d, fe.Evaluate(Cell(sheet, "B3")).NumberValue);

            ICell total = CreateCell(sheet, "D1");
            total.SetCellFormula("SUM(A1:B3)");
            ClassicAssert.AreEqual(21d, fe.Evaluate(total).NumberValue);
        }

        [Test]
        public void TestEvaluateAllSpills()
        {
            using var wb = new XSSFWorkbook();
            var sheet = (XSSFSheet) wb.CreateSheet();
            string[] fruit = { "Pear", "Apple", "Pear", "Fig" };
            for(int i = 0; i < fruit.Length; i++)
            {
                CreateCell(sheet, "A" + (i + 1)).SetCellValue(fruit[i]);
            }
            sheet.SetDynamicArrayFormula("_xlfn._xlws.SORT(_xlfn.UNIQUE(A1:A4))", new CellReference("C1"));
            sheet.SetDynamicArrayFormula("_xlfn._xlws.FILTER(A1:A4,A1:A4<>\"Pear\")", new CellReference("E1"));

            new XSSFFormulaEvaluator(wb).EvaluateAll();

            ClassicAssert.AreEqual("C1:C3", Cell(sheet, "C1").ArrayFormulaRange.FormatAsString());
            ClassicAssert.AreEqual("Apple", Cell(sheet, "C1").StringCellValue);
            ClassicAssert.AreEqual("Fig", Cell(sheet, "C2").StringCellValue);
            ClassicAssert.AreEqual("Pear", Cell(sheet, "C3").StringCellValue);
            ClassicAssert.AreEqual("E1:E2", Cell(sheet, "E1").ArrayFormulaRange.FormatAsString());
            ClassicAssert.AreEqual("Apple", Cell(sheet, "E1").StringCellValue);
            ClassicAssert.AreEqual("Fig", Cell(sheet, "E2").StringCellValue);
        }

        [Test]
        public void TestEvaluateAllRefreshesFormulasEvaluatedBeforeTheSpill()
        {
            using var wb = new XSSFWorkbook();
            var sheet = (XSSFSheet) wb.CreateSheet();
            ICell total = CreateCell(sheet, "A1");
            total.SetCellFormula("SUM(B1:B3)");
            sheet.SetDynamicArrayFormula("_xlfn.SEQUENCE(3)", new CellReference("B1"));

            XSSFFormulaEvaluator.EvaluateAllFormulaCells(wb);

            ClassicAssert.AreEqual(CellType.Numeric, total.CachedFormulaResultType);
            ClassicAssert.AreEqual(6d, total.NumericCellValue);
        }

        [Test]
        public void TestBlockedSpillGivesSpillError()
        {
            using var wb = new XSSFWorkbook();
            var sheet = (XSSFSheet) wb.CreateSheet();
            var fe = new XSSFFormulaEvaluator(wb);
            ICell anchor = sheet.SetDynamicArrayFormula("_xlfn.SEQUENCE(3)", new CellReference("A1"));
            ICell blocker = CreateCell(sheet, "A3");
            blocker.SetCellValue("in the way");

            ClassicAssert.AreEqual(CellType.Error, fe.EvaluateFormulaCell(anchor));
            ClassicAssert.AreEqual(FormulaError.SPILL.Code, anchor.ErrorCellValue);
            ClassicAssert.AreEqual("A1", anchor.ArrayFormulaRange.FormatAsString());
            ClassicAssert.IsNull(Cell(sheet, "A2"));
            ClassicAssert.AreEqual("in the way", blocker.StringCellValue);
            // the evaluator agrees, so formulas that read the anchor see #SPILL! too
            CellValue direct = fe.Evaluate(anchor);
            ClassicAssert.AreEqual(CellType.Error, direct.CellType);
            ClassicAssert.AreEqual(FormulaError.SPILL.Code, direct.ErrorValue);

            sheet.GetRow(2).RemoveCell(blocker);
            fe.ClearAllCachedResultValues();
            ClassicAssert.AreEqual(CellType.Numeric, fe.EvaluateFormulaCell(anchor));
            ClassicAssert.AreEqual("A1:A3", anchor.ArrayFormulaRange.FormatAsString());
            ClassicAssert.AreEqual(3d, Cell(sheet, "A3").NumericCellValue);
        }

        [Test]
        public void TestSpillBlockedByMergedRegionOrSheetEdge()
        {
            using var wb = new XSSFWorkbook();
            var sheet = (XSSFSheet) wb.CreateSheet();
            var fe = new XSSFFormulaEvaluator(wb);
            sheet.AddMergedRegion(CellRangeAddress.ValueOf("B2:C2"));
            ICell anchor = sheet.SetDynamicArrayFormula("_xlfn.SEQUENCE(1,3)", new CellReference("A2"));
            ClassicAssert.AreEqual(CellType.Error, fe.EvaluateFormulaCell(anchor));
            ClassicAssert.AreEqual(FormulaError.SPILL.Code, anchor.ErrorCellValue);

            ICell edge = sheet.SetDynamicArrayFormula("_xlfn.SEQUENCE(2)", new CellReference("A1048576"));
            ClassicAssert.AreEqual(CellType.Error, fe.EvaluateFormulaCell(edge));
            ClassicAssert.AreEqual(FormulaError.SPILL.Code, edge.ErrorCellValue);
        }

        [Test]
        public void TestSpillShrinksWhenResultShrinks()
        {
            using var wb = new XSSFWorkbook();
            var sheet = (XSSFSheet) wb.CreateSheet();
            var fe = new XSSFFormulaEvaluator(wb);
            ICell size = CreateCell(sheet, "C1");
            size.SetCellValue(3);
            ICell anchor = sheet.SetDynamicArrayFormula("_xlfn.SEQUENCE(C1)", new CellReference("A1"));
            fe.EvaluateFormulaCell(anchor);
            ClassicAssert.AreEqual("A1:A3", anchor.ArrayFormulaRange.FormatAsString());

            size.SetCellValue(1);
            fe.NotifyUpdateCell(size);
            fe.EvaluateFormulaCell(anchor);
            ClassicAssert.AreEqual("A1", anchor.ArrayFormulaRange.FormatAsString());
            ClassicAssert.AreEqual(1d, anchor.NumericCellValue);
            ClassicAssert.AreEqual(CellType.Blank, Cell(sheet, "A2").CellType);
            ClassicAssert.AreEqual(CellType.Blank, Cell(sheet, "A3").CellType);
        }

        [Test]
        public void TestEmptyFilterGivesCalcError()
        {
            using var wb = new XSSFWorkbook();
            var sheet = (XSSFSheet) wb.CreateSheet();
            CreateCell(sheet, "A1").SetCellValue(1);
            CreateCell(sheet, "A2").SetCellValue(2);
            ICell anchor = sheet.SetDynamicArrayFormula("_xlfn._xlws.FILTER(A1:A2,A1:A2>5)", new CellReference("C1"));
            ClassicAssert.AreEqual(CellType.Error, new XSSFFormulaEvaluator(wb).EvaluateFormulaCell(anchor));
            ClassicAssert.AreEqual(FormulaError.CALC.Code, anchor.ErrorCellValue);
            ClassicAssert.AreEqual("#CALC!", ((XSSFCell) anchor).GetCTCell().v);
        }

        [Test]
        public void TestLegacyFormulaDoesNotSpill()
        {
            using var wb = new XSSFWorkbook();
            var sheet = wb.CreateSheet();
            ICell cell = CreateCell(sheet, "A1");
            cell.SetCellFormula("_xlfn.SEQUENCE(3)");
            var fe = new XSSFFormulaEvaluator(wb);
            ClassicAssert.AreEqual(CellType.Numeric, fe.EvaluateFormulaCell(cell));
            ClassicAssert.AreEqual(1d, cell.NumericCellValue);
            ClassicAssert.IsFalse(cell.IsPartOfArrayFormulaGroup);
            ClassicAssert.IsNull(Cell(sheet, "A2"));
        }

        [Test]
        public void TestRoundTripKeepsDynamicArrayFormula()
        {
            using var wb = new XSSFWorkbook();
            var sheet = (XSSFSheet) wb.CreateSheet();
            CreateCell(sheet, "A1").SetCellValue("b");
            CreateCell(sheet, "A2").SetCellValue("a");
            ICell anchor = sheet.SetDynamicArrayFormula("_xlfn._xlws.SORT(A1:A2)", new CellReference("C1"));
            new XSSFFormulaEvaluator(wb).EvaluateFormulaCell(anchor);

            using var back = XSSFTestDataSamples.WriteOutAndReadBack(wb);
            AssertDynamicArray(back);
            OPCPackage pkg = back.Package;
            var parts = pkg.GetPartsByContentType(XSSFRelation.SHEET_METADATA.ContentType);
            ClassicAssert.AreEqual(1, parts.Count);
            using(var reader = new StreamReader(parts[0].GetInputStream(), Encoding.UTF8))
            {
                StringAssert.Contains("XLDAPR", reader.ReadToEnd());
            }

            // a second round trip keeps the metadata part read from the file
            using var again = XSSFTestDataSamples.WriteOutAndReadBack(back);
            AssertDynamicArray(again);
            ClassicAssert.AreEqual(1, again.Package.GetPartsByContentType(XSSFRelation.SHEET_METADATA.ContentType).Count);
        }

        private static void AssertDynamicArray(XSSFWorkbook wb)
        {
            ISheet sheet = wb.GetSheetAt(0);
            var anchor = (XSSFCell) Cell(sheet, "C1");
            ClassicAssert.AreEqual("_xlfn._xlws.SORT(A1:A2)", anchor.CellFormula);
            ClassicAssert.AreEqual("C1:C2", anchor.ArrayFormulaRange.FormatAsString());
            ClassicAssert.AreEqual(1u, anchor.GetCTCell().cm);
            ClassicAssert.IsTrue(anchor.IsDynamicArrayFormula);
            ClassicAssert.AreEqual("a", anchor.StringCellValue);
            ClassicAssert.AreEqual("b", Cell(sheet, "C2").StringCellValue);

            var fe = new XSSFFormulaEvaluator(wb);
            ClassicAssert.AreEqual("b", fe.Evaluate(Cell(sheet, "C2")).StringValue);
        }
    }
}