using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.SS.Formula;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace TestCases.SS.UserModel
{
    /// <summary>
    /// Top/bottom N conditional formats rank only the numbers in the range (Excel ignores text and blanks).
    /// </summary>
    [TestFixture]
    public class TestConditionalFormattingTop10
    {
        [TestCase(false, false, new[] { 2, 6 })] // top 2: 50, 40
        [TestCase(true, false, new[] { 1, 5 })]  // bottom 2: 10, 30
        [TestCase(false, true, new[] { 2, 6 })]  // top 50% of 4 numbers: 50, 40
        public void TestTop10IgnoresTextAndBlanks(bool bottom, bool percent, int[] expectedRows)
        {
            using XSSFWorkbook wb = new XSSFWorkbook();
            ISheet sheet = wb.CreateSheet();
            sheet.CreateRow(0).CreateCell(0).SetCellValue(10);
            sheet.CreateRow(1).CreateCell(0).SetCellValue(50);
            sheet.CreateRow(2).CreateCell(0).SetCellValue("x");
            sheet.CreateRow(3); // blank cell A4
            sheet.CreateRow(4).CreateCell(0).SetCellValue(30);
            sheet.CreateRow(5).CreateCell(0).SetCellValue(40);

            XSSFSheetConditionalFormatting scf = (XSSFSheetConditionalFormatting) sheet.SheetConditionalFormatting;
            XSSFConditionalFormattingRule rule = (XSSFConditionalFormattingRule) scf.CreateConditionalFormattingRule("TRUE");
            CT_CfRule ct = rule.GetCTCfRule();
            ct.type = ST_CfType.top10;
            ct.rank = percent ? 50u : 2u;
            ct.bottom = bottom;
            ct.percent = percent;
            scf.AddConditionalFormatting(new[] { CellRangeAddress.ValueOf("A1:A6") }, rule);

            ConditionalFormattingEvaluator cfe = new ConditionalFormattingEvaluator(wb, new XSSFFormulaEvaluator(wb));
            for(int row = 1; row <= 6; row++)
            {
                bool expected = System.Array.IndexOf(expectedRows, row) >= 0;
                int matches = cfe.GetConditionalFormattingForCell(new CellReference(sheet.SheetName, row - 1, 0, false, false)).Count;
                ClassicAssert.AreEqual(expected ? 1 : 0, matches, "A" + row);
            }
        }
    }
}