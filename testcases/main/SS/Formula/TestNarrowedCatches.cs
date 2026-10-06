namespace TestCases.SS.Formula
{
    using NPOI.HSSF.UserModel;
    using NPOI.SS.Format;
    using NPOI.SS.Formula.Eval;
    using NPOI.SS.UserModel;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;

    /// <summary>
    /// Fallback results at sites whose broad catches were narrowed (task #123).
    /// </summary>
    [TestFixture]
    public class TestNarrowedCatches
    {
        private static HSSFFormulaEvaluator NewEvaluator(out ICell cell)
        {
            HSSFWorkbook wb = new HSSFWorkbook();
            ISheet sheet = wb.CreateSheet("S");
            for(int i = 0; i < 3; i++)
                sheet.CreateRow(i).CreateCell(0).SetCellValue(i + 1);
            cell = sheet.GetRow(0).CreateCell(2);
            return new HSSFFormulaEvaluator(wb);
        }

        [Test]
        public void TestXMatchInvalidModesGiveValueError()
        {
            HSSFFormulaEvaluator fe = NewEvaluator(out ICell cell);
            Util.Utils.AssertError(fe, cell, "XMATCH(2,A1:A3,99)", FormulaError.VALUE);
            Util.Utils.AssertError(fe, cell, "XMATCH(2,A1:A3,0,99)", FormulaError.VALUE);
        }

        [Test]
        public void TestXLookupInvalidModesGiveValueError()
        {
            HSSFFormulaEvaluator fe = NewEvaluator(out ICell cell);
            Util.Utils.AssertError(fe, cell, "XLOOKUP(2,A1:A3,A1:A3,\"x\",99)", FormulaError.VALUE);
            Util.Utils.AssertError(fe, cell, "XLOOKUP(2,A1:A3,A1:A3,\"x\",0,99)", FormulaError.VALUE);
        }

        [Test]
        public void TestNumberValueUnparsableGivesValueError()
        {
            HSSFFormulaEvaluator fe = NewEvaluator(out ICell cell);
            Util.Utils.AssertError(fe, cell, "NUMBERVALUE(\"abc\")", FormulaError.VALUE);
        }

        [Test]
        public void TestParseDoubleUnparsableIsNaN()
        {
            ClassicAssert.IsTrue(double.IsNaN(OperandResolver.ParseDouble("not a number")));
            ClassicAssert.IsTrue(double.IsNaN(OperandResolver.ParseDouble("")));
            ClassicAssert.AreEqual(12500.0, OperandResolver.ParseDouble("1.25E4"));
        }

        [Test]
        public void TestAverageifTwoAndThreeArgs()
        {
            HSSFFormulaEvaluator fe = NewEvaluator(out ICell cell);
            Util.Utils.AssertDouble(fe, cell, "AVERAGEIF(A1:A3,\">1\")", 2.5);
            Util.Utils.AssertDouble(fe, cell, "AVERAGEIF(A1:A3,\">1\",A1:A3)", 2.5);
        }

        [Test]
        public void TestCellNumberStringModEqualsOtherTypes()
        {
            CellNumberStringMod mod = new CellNumberStringMod(new CellNumberFormatter.Special('0', 1), "x", CellNumberStringMod.AFTER);
            CellNumberStringMod same = new CellNumberStringMod(new CellNumberFormatter.Special('0', 1), "y", CellNumberStringMod.AFTER);
            ClassicAssert.IsFalse(mod.Equals(null));
            ClassicAssert.IsFalse(mod.Equals("not a mod"));
            ClassicAssert.IsTrue(mod.Equals(same));
        }

        [Test]
        public void TestCellFormatInvalidPartFallsBack()
        {
            // an invalid part is logged and becomes null instead of failing construction
            Assert.DoesNotThrow(() => CellFormat.GetInstance("0.00;[bogus"));
        }
    }
}
