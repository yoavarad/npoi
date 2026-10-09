using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using TestCases.HSSF;

namespace TestCases.SS.Formula.Functions
{
    /// <summary>
    /// Date functions must honor the workbook's 1904 date system (Excel: serial 0 is 1904-01-01).
    /// </summary>
    [TestFixture]
    public class TestDate1904Windowing
    {
        private HSSFWorkbook wb;
        private ICell cell;
        private IFormulaEvaluator evaluator;

        [SetUp]
        public void SetUp()
        {
            wb = HSSFTestDataSamples.OpenSampleWorkbook("1904DateWindowing.xls");
            ClassicAssert.IsTrue(wb.IsDate1904());
            cell = wb.GetSheetAt(0).CreateRow(100).CreateCell(0);
            evaluator = wb.GetCreationHelper().CreateFormulaEvaluator();
        }

        [TearDown]
        public void TearDown()
        {
            wb.Close();
        }

        private CellValue Eval(string formula)
        {
            cell.SetCellFormula(formula);
            evaluator.ClearAllCachedResultValues();
            return evaluator.Evaluate(cell);
        }

        private void ConfirmNumber(string formula, double expected)
        {
            CellValue cv = Eval(formula);
            ClassicAssert.AreEqual(CellType.Numeric, cv.CellType, formula);
            ClassicAssert.AreEqual(expected, cv.NumberValue, 0.0, formula);
        }

        [Test]
        public void TestDateFunction()
        {
            ConfirmNumber("DATE(1904,1,1)", 0);
            ConfirmNumber("DATE(1904,1,2)", 1);
            ConfirmNumber("DATE(2000,1,1)", 35064);
            ConfirmNumber("DATE(1904,3,1)", 60);
            CellValue cv = Eval("DATE(1903,12,31)");
            ClassicAssert.AreEqual(CellType.Error, cv.CellType);
            ClassicAssert.AreEqual(FormulaError.NUM.Code, cv.ErrorValue);
        }

        [Test]
        public void TestCalendarFields()
        {
            ConfirmNumber("YEAR(0)", 1904);
            ConfirmNumber("MONTH(0)", 1);
            ConfirmNumber("DAY(0)", 1);
            ConfirmNumber("YEAR(35064)", 2000);
            ConfirmNumber("DAY(DATE(2010,6,15))", 15);
            ConfirmNumber("HOUR(0.5)", 12);
        }

        [Test]
        public void TestTextDateFormat()
        {
            CellValue cv = Eval("TEXT(0,\"yyyy-mm-dd\")");
            ClassicAssert.AreEqual("1904-01-01", cv.StringValue);
            cv = Eval("TEXT(35064,\"yyyy-mm-dd\")");
            ClassicAssert.AreEqual("2000-01-01", cv.StringValue);
        }
    }
}