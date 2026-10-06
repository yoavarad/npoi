namespace TestCases.HSSF.Util
{
    using NPOI.HSSF.UserModel;
    using NPOI.HSSF.Util;
    using NPOI.SS.UserModel;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System.Collections.Generic;

    [TestFixture]
    public class TestHSSFCellUtil
    {
        [Test]
        public void TestCopyCellFormulaWithStringResultFallsBackToString()
        {
            HSSFWorkbook wb = new HSSFWorkbook();
            HSSFSheet sheet = (HSSFSheet) wb.CreateSheet("S");
            HSSFCell oldCell = (HSSFCell) sheet.CreateRow(0).CreateCell(0);
            oldCell.CellFormula = "\"abc\"";
            new HSSFFormulaEvaluator(wb).EvaluateFormulaCell(oldCell);
            HSSFCell newCell = (HSSFCell) sheet.CreateRow(1).CreateCell(0);

            // NumericCellValue on a string-valued formula throws InvalidOperationException;
            // CopyCell falls back to copying the text.
            HSSFCellUtil.CopyCell(oldCell, newCell, null, new Dictionary<short, short>(), false);

            ClassicAssert.AreEqual(CellType.String, newCell.CellType);
            ClassicAssert.AreEqual(oldCell.ToString(), newCell.StringCellValue);
        }

        [Test]
        public void TestCopyCellFormulaWithNumericResultCopiesNumber()
        {
            HSSFWorkbook wb = new HSSFWorkbook();
            HSSFSheet sheet = (HSSFSheet) wb.CreateSheet("S");
            HSSFCell oldCell = (HSSFCell) sheet.CreateRow(0).CreateCell(0);
            oldCell.CellFormula = "1+2";
            new HSSFFormulaEvaluator(wb).EvaluateFormulaCell(oldCell);
            HSSFCell newCell = (HSSFCell) sheet.CreateRow(1).CreateCell(0);

            HSSFCellUtil.CopyCell(oldCell, newCell, null, new Dictionary<short, short>(), false);

            ClassicAssert.AreEqual(CellType.Numeric, newCell.CellType);
            ClassicAssert.AreEqual(3.0, newCell.NumericCellValue);
        }
    }
}