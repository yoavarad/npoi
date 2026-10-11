namespace TestCases.SS.Formula.Functions
{
    using NPOI.HSSF.UserModel;
    using NPOI.SS.UserModel;
    using NUnit.Framework;
    using TestCases.SS.Util;

    /// <summary>
    /// D* functions with computed (formula) criteria.
    /// </summary>
    [TestFixture]
    public class TestDStarComputedCriteria
    {
        // Database A4:E10 (header row 4, data rows 5-10); average Height = 12.33
        private static HSSFWorkbook InitWorkbook(string formula, string secondHeader, string secondCriterion)
        {
            HSSFWorkbook wb = new HSSFWorkbook();
            HSSFSheet sheet = wb.CreateSheet() as HSSFSheet;
            Utils.AddRow(sheet, 0, null, secondHeader);
            Utils.AddRow(sheet, 1, null, secondCriterion);
            sheet.GetRow(1).GetCell(0).SetCellFormula(formula);
            Utils.AddRow(sheet, 2);
            Utils.AddRow(sheet, 3, "Tree", "Height", "Age", "Yield", "Profit");
            Utils.AddRow(sheet, 4, "Apple", 18, 20, 14, 105);
            Utils.AddRow(sheet, 5, "Pear", 12, 12, 10, 96);
            Utils.AddRow(sheet, 6, "Cherry", 13, 14, 9, 105);
            Utils.AddRow(sheet, 7, "Apple", 14, 15, 10, 75);
            Utils.AddRow(sheet, 8, "Pear", 9, 8, 8, 76.8);
            Utils.AddRow(sheet, 9, "Apple", 8, 9, 6, 45);
            return wb;
        }

        [Test]
        public void TestRelativeAndAbsoluteRefs()
        {
            using(HSSFWorkbook wb = InitWorkbook("B5>AVERAGE($B$5:$B$10)", null, null))
            {
                HSSFFormulaEvaluator fe = new HSSFFormulaEvaluator(wb);
                ICell cell = wb.GetSheetAt(0).GetRow(0).CreateCell(12);
                Utils.AssertDouble(fe, cell, "DSUM(A4:E10, \"Yield\", A1:A2)", 33);
                Utils.AssertDouble(fe, cell, "DCOUNT(A4:E10, \"Yield\", A1:A2)", 3);
                Utils.AssertError(fe, cell, "DGET(A4:E10, \"Age\", A1:A2)", FormulaError.NUM);
            }
        }

        [Test]
        public void TestMixedWithNormalColumn()
        {
            using(HSSFWorkbook wb = InitWorkbook("B5>AVERAGE($B$5:$B$10)", "Tree", "Apple"))
            {
                HSSFFormulaEvaluator fe = new HSSFFormulaEvaluator(wb);
                ICell cell = wb.GetSheetAt(0).GetRow(0).CreateCell(12);
                Utils.AssertDouble(fe, cell, "DSUM(A4:E10, \"Yield\", A1:B2)", 24);
            }
        }
    }
}