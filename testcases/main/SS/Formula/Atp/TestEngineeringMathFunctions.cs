using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NUnit.Framework;

namespace TestCases.SS.Formula.Atp
{
    /// <summary>
    /// Tests GCD, LCM, SQRTPI, EFFECT, NOMINAL, MULTINOMIAL, GESTEP; expected values are Excel results.
    /// </summary>
    [TestFixture]
    public class TestEngineeringMathFunctions
    {
        private static void Run(System.Action<HSSFFormulaEvaluator, ICell> body)
        {
            using(HSSFWorkbook wb = new HSSFWorkbook())
            {
                ISheet sheet = wb.CreateSheet();
                SS.Util.Utils.AddRow(sheet, 0, 24, 36, 60);
                ICell cell = sheet.CreateRow(5).CreateCell(0);
                body(new HSSFFormulaEvaluator(wb), cell);
            }
        }

        [Test]
        public void TestGcd()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "GCD(5,2)", 1);
                SS.Util.Utils.AssertDouble(fe, c, "GCD(24,36)", 12);
                SS.Util.Utils.AssertDouble(fe, c, "GCD(7,1)", 1);
                SS.Util.Utils.AssertDouble(fe, c, "GCD(5,0)", 5);
                SS.Util.Utils.AssertDouble(fe, c, "GCD(A1:C1)", 12);
                SS.Util.Utils.AssertDouble(fe, c, "GCD(9.9,6)", 3);
                SS.Util.Utils.AssertError(fe, c, "GCD(-1,2)", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "GCD(\"x\",2)", FormulaError.VALUE);
            });
        }

        [Test]
        public void TestLcm()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "LCM(5,2)", 10);
                SS.Util.Utils.AssertDouble(fe, c, "LCM(24,36)", 72);
                SS.Util.Utils.AssertDouble(fe, c, "LCM(A1:C1)", 360);
                SS.Util.Utils.AssertDouble(fe, c, "LCM(5,0)", 0);
                SS.Util.Utils.AssertError(fe, c, "LCM(-1,2)", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "LCM(0,-1)", FormulaError.NUM);
            });
        }

        [Test]
        public void TestSqrtPi()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "SQRTPI(1)", 1.7724538509055159, 1e-12);
                SS.Util.Utils.AssertDouble(fe, c, "SQRTPI(2)", 2.5066282746310002, 1e-12);
                SS.Util.Utils.AssertDouble(fe, c, "SQRTPI(0)", 0);
                SS.Util.Utils.AssertError(fe, c, "SQRTPI(-1)", FormulaError.NUM);
            });
        }

        [Test]
        public void TestEffectNominal()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "EFFECT(0.0525,4)", 0.05354266, 1e-8);
                SS.Util.Utils.AssertDouble(fe, c, "NOMINAL(0.053543,4)", 0.05250032, 1e-8);
                SS.Util.Utils.AssertError(fe, c, "EFFECT(0.05,0)", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "EFFECT(-0.05,4)", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "NOMINAL(0,4)", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "NOMINAL(0.05,0.5)", FormulaError.NUM);
            });
        }

        [Test]
        public void TestMultinomial()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "MULTINOMIAL(2,3,4)", 1260);
                SS.Util.Utils.AssertDouble(fe, c, "MULTINOMIAL(1,1)", 2);
                SS.Util.Utils.AssertError(fe, c, "MULTINOMIAL(-1,2)", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "MULTINOMIAL(1E15)", FormulaError.NUM);
            });
        }

        [Test]
        public void TestGeStep()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "GESTEP(5,4)", 1);
                SS.Util.Utils.AssertDouble(fe, c, "GESTEP(5,5)", 1);
                SS.Util.Utils.AssertDouble(fe, c, "GESTEP(-4,-5)", 1);
                SS.Util.Utils.AssertDouble(fe, c, "GESTEP(-1,0)", 0);
                SS.Util.Utils.AssertDouble(fe, c, "GESTEP(1)", 1);
                SS.Util.Utils.AssertDouble(fe, c, "GESTEP(-1)", 0);
            });
        }

        [Test]
        public void TestXLookupBackwardAndNotFound()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AddRow(c.Sheet, 1, 1, 2, 1);
                SS.Util.Utils.AddRow(c.Sheet, 2, 10, 20, 30);
                SS.Util.Utils.AssertDouble(fe, c, "XLOOKUP(1,A2:C2,A3:C3)", 10);
                SS.Util.Utils.AssertDouble(fe, c, "XLOOKUP(1,A2:C2,A3:C3,,0,-1)", 30);
                SS.Util.Utils.AssertDouble(fe, c, "XLOOKUP(5,A2:C2,A3:C3,-7)", -7);
                SS.Util.Utils.AssertDouble(fe, c, "XLOOKUP(5,A2:C2,A3:C3,,-1)", 20);
                SS.Util.Utils.AssertError(fe, c, "XLOOKUP(5,A2:C2,A3:C3)", FormulaError.NA);
            });
        }
    }
}