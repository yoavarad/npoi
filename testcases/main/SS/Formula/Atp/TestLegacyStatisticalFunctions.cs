using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NUnit.Framework;

namespace TestCases.SS.Formula.Atp
{
    /// <summary>
    /// Tests the legacy statistical built-ins (CORREL PEARSON RSQ COVAR STEYX SKEW KURT GEOMEAN HARMEAN
    /// QUARTILE TRIMMEAN AVERAGEA STDEVA STDEVPA VARA VARPA FISHER FISHERINV PERMUT); expected values are
    /// the Microsoft documentation examples.
    /// </summary>
    [TestFixture]
    public class TestLegacyStatisticalFunctions
    {
        private static void Run(System.Action<HSSFFormulaEvaluator, ICell> body)
        {
            using(HSSFWorkbook wb = new HSSFWorkbook())
            {
                ISheet sheet = wb.CreateSheet();
                SS.Util.Utils.AddRow(sheet, 0, 2, 3, 9, 1, 8, 7, 5);        // A1:G1 y
                SS.Util.Utils.AddRow(sheet, 1, 6, 5, 11, 7, 5, 4, 4);       // A2:G2 x
                SS.Util.Utils.AddRow(sheet, 2, 3, 4, 5, 2, 3, 4, 5, 6, 4, 7); // A3:J3 skew/kurt data
                SS.Util.Utils.AddRow(sheet, 3, 4, 5, 8, 7, 11, 4, 3);       // A4:G4 means
                SS.Util.Utils.AddRow(sheet, 4, 1345, 1301, 1368, 1322, 1310, 1370, 1318, 1350, 1303, 1299); // A5:J5
                SS.Util.Utils.AddRow(sheet, 5, 1, 2, 4, 7, 8, 9, 10, 12);   // A6:H6 quartile
                SS.Util.Utils.AddRow(sheet, 6, 4, 5, 6, 7, 2, 3, 4, 5, 1, 2, 3); // A7:K7 trimmean
                IRow r8 = sheet.CreateRow(7);
                r8.CreateCell(0).SetCellValue(10);
                r8.CreateCell(1).SetCellValue(7);
                r8.CreateCell(2).SetCellValue(9);
                r8.CreateCell(3).SetCellValue(2);
                r8.CreateCell(4).SetCellValue("Not available");
                r8.CreateCell(5).SetCellValue(true);
                ICell cell = sheet.CreateRow(20).CreateCell(0);
                body(new HSSFFormulaEvaluator(wb), cell);
            }
        }

        [Test]
        public void TestPairedFunctions()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "RSQ(A1:G1,A2:G2)", 0.05795019, 1e-6);
                SS.Util.Utils.AssertDouble(fe, c, "CORREL(A1:G1,A2:G2)", System.Math.Sqrt(0.05795019), 1e-6);
                SS.Util.Utils.AssertDouble(fe, c, "PEARSON(A1:G1,A2:G2)", System.Math.Sqrt(0.05795019), 1e-6);
                SS.Util.Utils.AssertDouble(fe, c, "STEYX(A1:G1,A2:G2)", 3.305719, 1e-5);
                SS.Util.Utils.AssertDouble(fe, c, "COVAR(A1:E1,A2:E2)", 1.0 * (
                    (2 - 4.6) * (6 - 6.8) + (3 - 4.6) * (5 - 6.8) + (9 - 4.6) * (11 - 6.8)
                    + (1 - 4.6) * (7 - 6.8) + (8 - 4.6) * (5 - 6.8)) / 5, 1e-9);
                SS.Util.Utils.AssertError(fe, c, "CORREL(A1:G1,A2:F2)", FormulaError.NA);
                SS.Util.Utils.AssertError(fe, c, "CORREL(A1,A2)", FormulaError.DIV0);
            });
        }

        [Test]
        public void TestShapeAndMeans()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "SKEW(A3:J3)", 0.359543, 1e-6);
                SS.Util.Utils.AssertDouble(fe, c, "KURT(A3:J3)", -0.151799637, 1e-8);
                SS.Util.Utils.AssertDouble(fe, c, "GEOMEAN(A4:G4)", 5.476987, 1e-6);
                SS.Util.Utils.AssertDouble(fe, c, "HARMEAN(A4:G4)", 5.028376, 1e-6);
                SS.Util.Utils.AssertError(fe, c, "GEOMEAN(A4:G4,0)", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "SKEW(1,2)", FormulaError.DIV0);
                SS.Util.Utils.AssertDouble(fe, c, "GEOMEAN(A4,B4,Z99)", System.Math.Sqrt(20), 1e-9);
            });
        }

        [Test]
        public void TestQuartileTrimMean()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "QUARTILE(A6:H6,0)", 1);
                SS.Util.Utils.AssertDouble(fe, c, "QUARTILE(A6:H6,1)", 3.5);
                SS.Util.Utils.AssertDouble(fe, c, "QUARTILE(A6:H6,2)", 7.5);
                SS.Util.Utils.AssertDouble(fe, c, "QUARTILE(A6:H6,3)", 9.25);
                SS.Util.Utils.AssertDouble(fe, c, "QUARTILE(A6:H6,4)", 12);
                SS.Util.Utils.AssertError(fe, c, "QUARTILE(A6:H6,5)", FormulaError.NUM);
                SS.Util.Utils.AssertDouble(fe, c, "TRIMMEAN(A7:K7,0.2)", 3.777778, 1e-6);
                SS.Util.Utils.AssertError(fe, c, "TRIMMEAN(A7:K7,1)", FormulaError.NUM);
            });
        }

        [Test]
        public void TestAFunctions()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "AVERAGEA(A8:E8)", 5.6, 1e-9);
                SS.Util.Utils.AssertDouble(fe, c, "AVERAGEA(A8:F8)", 29.0 / 6, 1e-9);
                SS.Util.Utils.AssertDouble(fe, c, "STDEVA(A5:J5)", 27.46392, 1e-4);
                SS.Util.Utils.AssertDouble(fe, c, "STDEVPA(A5:J5)", 26.05456, 1e-4);
                SS.Util.Utils.AssertDouble(fe, c, "VARA(A5:J5)", 754.2667, 1e-3);
                SS.Util.Utils.AssertDouble(fe, c, "VARPA(A5:J5)", 678.84, 1e-3);
                SS.Util.Utils.AssertError(fe, c, "STDEVA(1)", FormulaError.DIV0);
                SS.Util.Utils.AssertError(fe, c, "AVERAGEA(Z100:Z101)", FormulaError.DIV0);
            });
        }

        [Test]
        public void TestFisherPermut()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "FISHER(0.75)", 0.972955, 1e-6);
                SS.Util.Utils.AssertDouble(fe, c, "FISHERINV(0.972955)", 0.75, 1e-6);
                SS.Util.Utils.AssertError(fe, c, "FISHER(1)", FormulaError.NUM);
                SS.Util.Utils.AssertDouble(fe, c, "PERMUT(100,3)", 970200);
                SS.Util.Utils.AssertDouble(fe, c, "PERMUT(3,2)", 6);
                SS.Util.Utils.AssertError(fe, c, "PERMUT(2,3)", FormulaError.NUM);
            });
        }
    }
}