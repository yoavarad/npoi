using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace TestCases.XSSF.UserModel
{
    /// <summary>
    /// The XSSF evaluator passes the workbook's 1904 date system to date functions.
    /// </summary>
    [TestFixture]
    public class TestXSSFDate1904Evaluation
    {
        [Test]
        public void TestDateFunctionsUse1904Windowing()
        {
            using XSSFWorkbook wb = new XSSFWorkbook();
            wb.GetCTWorkbook().workbookPr.date1904 = true;
            ClassicAssert.IsTrue(wb.IsDate1904());
            ICell cell = wb.CreateSheet().CreateRow(0).CreateCell(0);
            IFormulaEvaluator evaluator = wb.GetCreationHelper().CreateFormulaEvaluator();

            cell.SetCellFormula("DATE(2000,1,1)");
            ClassicAssert.AreEqual(35064, evaluator.Evaluate(cell).NumberValue, 0.0);

            cell.SetCellFormula("YEAR(0)");
            evaluator.ClearAllCachedResultValues();
            ClassicAssert.AreEqual(1904, evaluator.Evaluate(cell).NumberValue, 0.0);
        }
    }
}