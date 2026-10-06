using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NUnit.Framework;

namespace TestCases.SS.Formula.Atp
{
    /// <summary>Tests base-conversion, ERF/ERFC and IM* functions; expected values are Excel results.</summary>
    [TestFixture]
    public class TestEngineeringConversionFunctions
    {
        private static void Run(System.Action<HSSFFormulaEvaluator, ICell> body)
        {
            using(HSSFWorkbook wb = new HSSFWorkbook())
            {
                ISheet sheet = wb.CreateSheet();
                ICell cell = sheet.CreateRow(5).CreateCell(0);
                body(new HSSFFormulaEvaluator(wb), cell);
            }
        }

        [Test]
        public void TestBaseConversions()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertString(fe, c, "BIN2HEX(\"11111011\",4)", "00FB");
                SS.Util.Utils.AssertString(fe, c, "BIN2HEX(\"1110\")", "E");
                SS.Util.Utils.AssertString(fe, c, "BIN2HEX(\"1111111111\")", "FFFFFFFFFF");
                SS.Util.Utils.AssertString(fe, c, "BIN2OCT(\"1001\",3)", "011");
                SS.Util.Utils.AssertString(fe, c, "BIN2OCT(\"1111111111\")", "7777777777");
                SS.Util.Utils.AssertString(fe, c, "DEC2OCT(58,3)", "072");
                SS.Util.Utils.AssertString(fe, c, "DEC2OCT(-100)", "7777777634");
                SS.Util.Utils.AssertString(fe, c, "HEX2BIN(\"F\",8)", "00001111");
                SS.Util.Utils.AssertString(fe, c, "HEX2BIN(\"FFFFFFFFFF\")", "1111111111");
                SS.Util.Utils.AssertString(fe, c, "HEX2OCT(\"F\",3)", "017");
                SS.Util.Utils.AssertString(fe, c, "HEX2OCT(\"FFFFFFFF00\")", "7777777400");
                SS.Util.Utils.AssertString(fe, c, "OCT2BIN(\"3\",3)", "011");
                SS.Util.Utils.AssertString(fe, c, "OCT2BIN(\"7777777000\")", "1000000000");
                SS.Util.Utils.AssertString(fe, c, "OCT2HEX(\"100\",4)", "0040");
                SS.Util.Utils.AssertString(fe, c, "OCT2HEX(\"7777777533\")", "FFFFFFFF5B");
                SS.Util.Utils.AssertError(fe, c, "BIN2HEX(\"2\")", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "HEX2BIN(\"G\")", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "HEX2BIN(\"200\")", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "OCT2BIN(\"1000\")", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "BIN2HEX(\"11111011\",1)", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "DEC2OCT(\"x\")", FormulaError.VALUE);
                SS.Util.Utils.AssertError(fe, c, "DEC2OCT(549755813888)", FormulaError.NUM);
            });
        }

        [Test]
        public void TestErf()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "ERF(0)", 0, 1e-14);
                SS.Util.Utils.AssertDouble(fe, c, "ERF(0.745)", 0.707929, 1e-6);
                SS.Util.Utils.AssertDouble(fe, c, "ERF(1)", 0.842700792949715, 1e-13);
                SS.Util.Utils.AssertDouble(fe, c, "ERF(-1)", -0.842700792949715, 1e-13);
                SS.Util.Utils.AssertDouble(fe, c, "ERF(0,1)", 0.842700792949715, 1e-13);
                SS.Util.Utils.AssertDouble(fe, c, "ERF(4)", 0.999999984582742, 1e-13);
                SS.Util.Utils.AssertDouble(fe, c, "ERFC(1)", 0.157299207050285, 1e-13);
                SS.Util.Utils.AssertDouble(fe, c, "ERFC(0)", 1, 1e-14);
                SS.Util.Utils.AssertDouble(fe, c, "ERFC(-1)", 1.842700792949715, 1e-13);
                SS.Util.Utils.AssertDouble(fe, c, "ERFC(5)", 1.53745979442803E-12, 1e-24);
                SS.Util.Utils.AssertError(fe, c, "ERF(\"x\")", FormulaError.VALUE);
            });
        }

        [Test]
        public void TestComplex()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "IMABS(\"5+12i\")", 13, 1e-12);
                SS.Util.Utils.AssertDouble(fe, c, "IMARGUMENT(\"3+4i\")", 0.927295218001612, 1e-12);
                SS.Util.Utils.AssertError(fe, c, "IMARGUMENT(0)", FormulaError.DIV0);
                SS.Util.Utils.AssertString(fe, c, "IMCONJUGATE(\"3+4i\")", "3-4i");
                SS.Util.Utils.AssertString(fe, c, "IMSUM(\"3+4i\",\"5-3i\")", "8+i");
                SS.Util.Utils.AssertString(fe, c, "IMSUB(\"13+4i\",\"5+3i\")", "8+i");
                SS.Util.Utils.AssertString(fe, c, "IMPRODUCT(\"3+4i\",\"5-3i\")", "27+11i");
                SS.Util.Utils.AssertString(fe, c, "IMDIV(\"-238+240i\",\"10+24i\")", "5+12i");
                SS.Util.Utils.AssertError(fe, c, "IMDIV(\"1+i\",0)", FormulaError.NUM);
                SS.Util.Utils.AssertString(fe, c, "IMSQRT(\"1+i\")", "1.09868411346781+0.455089860562227i");
                SS.Util.Utils.AssertString(fe, c, "IMSQRT(\"-4\")", "2i");
                SS.Util.Utils.AssertString(fe, c, "IMEXP(\"1+i\")", "1.46869393991589+2.28735528717884i");
                SS.Util.Utils.AssertString(fe, c, "IMLN(\"3+4i\")", "1.6094379124341+0.927295218001612i");
                SS.Util.Utils.AssertString(fe, c, "IMLOG10(\"3+4i\")", "0.698970004336019+0.402719196273373i");
                SS.Util.Utils.AssertString(fe, c, "IMLOG2(\"3+4i\")", "2.32192809488736+1.33780421245098i");
                SS.Util.Utils.AssertString(fe, c, "IMPOWER(\"2+3i\",3)", "-46+9i");
                SS.Util.Utils.AssertString(fe, c, "IMSIN(\"3+4i\")", "3.85373803791938-27.0168132580039i");
                SS.Util.Utils.AssertString(fe, c, "IMCOS(\"1+i\")", "0.833730025131149-0.988897705762865i");
                SS.Util.Utils.AssertString(fe, c, "IMSUM(\"1+2j\",\"3+4j\")", "4+6j");
                SS.Util.Utils.AssertError(fe, c, "IMSUM(\"1+2i\",\"3+4j\")", FormulaError.VALUE);
                SS.Util.Utils.AssertError(fe, c, "IMABS(\"abc\")", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "IMLN(0)", FormulaError.NUM);
            });
        }
    }
}