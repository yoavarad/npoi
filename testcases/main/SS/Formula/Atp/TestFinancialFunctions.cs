using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NUnit.Framework;

namespace TestCases.SS.Formula.Atp
{
    /// <summary>
    /// Tests depreciation (SLN SYD DB DDB VDB) and bond/security functions; expected values are
    /// Excel results (Microsoft documentation examples).
    /// </summary>
    [TestFixture]
    public class TestFinancialFunctions
    {
        private static void Run(System.Action<HSSFFormulaEvaluator, ICell> body)
        {
            using(HSSFWorkbook wb = new HSSFWorkbook())
            {
                ISheet sheet = wb.CreateSheet();
                // A1:E1 cash flows, A2:E2 dates (2008-01-01, 03-01, 10-30, 2009-02-15, 04-01)
                SS.Util.Utils.AddRow(sheet, 0, -10000, 2750, 4250, 3250, 2750);
                SS.Util.Utils.AddRow(sheet, 1, 39448, 39508, 39751, 39859, 39904);
                SS.Util.Utils.AddRow(sheet, 2, 0.09, 0.11, 0.1);
                ICell cell = sheet.CreateRow(5).CreateCell(0);
                body(new HSSFFormulaEvaluator(wb), cell);
            }
        }

        [Test]
        public void TestDepreciation()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "SLN(30000,7500,10)", 2250);
                SS.Util.Utils.AssertError(fe, c, "SLN(1,1,0)", FormulaError.DIV0);
                SS.Util.Utils.AssertDouble(fe, c, "SYD(30000,7500,10,1)", 4090.909090909091, 1e-9);
                SS.Util.Utils.AssertDouble(fe, c, "SYD(30000,7500,10,10)", 409.0909090909091, 1e-9);
                SS.Util.Utils.AssertError(fe, c, "SYD(30000,7500,10,11)", FormulaError.NUM);

                SS.Util.Utils.AssertDouble(fe, c, "DB(1000000,100000,6,1,7)", 186083.33, 0.005);
                SS.Util.Utils.AssertDouble(fe, c, "DB(1000000,100000,6,2,7)", 259639.4165, 0.005);
                SS.Util.Utils.AssertDouble(fe, c, "DB(1000000,100000,6,3,7)", 176814.4427, 0.005);
                SS.Util.Utils.AssertDouble(fe, c, "DB(1000000,100000,6,6,7)", 55841.7561, 0.005);
                SS.Util.Utils.AssertDouble(fe, c, "DB(1000000,100000,6,7,7)", 15845.0985, 0.005);
                SS.Util.Utils.AssertError(fe, c, "DB(1000000,100000,6,8,7)", FormulaError.NUM);

                SS.Util.Utils.AssertDouble(fe, c, "DDB(2400,300,10*365,1)", 1.32, 0.005);
                SS.Util.Utils.AssertDouble(fe, c, "DDB(2400,300,10*12,1,2)", 40, 0.005);
                SS.Util.Utils.AssertDouble(fe, c, "DDB(2400,300,10,1,2)", 480, 0.005);
                SS.Util.Utils.AssertDouble(fe, c, "DDB(2400,300,10,2,1.5)", 306, 0.005);
                SS.Util.Utils.AssertDouble(fe, c, "DDB(2400,300,10,10)", 22.1225, 0.005);
                SS.Util.Utils.AssertError(fe, c, "DDB(2400,300,10,11)", FormulaError.NUM);

                SS.Util.Utils.AssertDouble(fe, c, "VDB(2400,300,10*365,0,1)", 1.32, 0.005);
                SS.Util.Utils.AssertDouble(fe, c, "VDB(2400,300,10*12,0,1)", 40, 0.005);
                SS.Util.Utils.AssertDouble(fe, c, "VDB(2400,300,10,0,1)", 480, 0.005);
                SS.Util.Utils.AssertDouble(fe, c, "VDB(2400,300,10*12,6,18)", 396.3060, 0.005);
                SS.Util.Utils.AssertDouble(fe, c, "VDB(2400,300,10*12,6,18,1.5)", 311.8089, 0.005);
                SS.Util.Utils.AssertError(fe, c, "VDB(2400,300,10,5,11)", FormulaError.NUM);
            });
        }

        [Test]
        public void TestSingleCashFlowSecurities()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "DISC(39107,39248,97.975,100,1)", 0.052420213, 1e-8);
                SS.Util.Utils.AssertDouble(fe, c, "PRICEDISC(39494,39508,0.0525,100,2)", 99.79583333, 1e-7);
                SS.Util.Utils.AssertDouble(fe, c, "YIELDDISC(39494,39508,99.795,100,2)", 0.052823, 1e-6);
                SS.Util.Utils.AssertDouble(fe, c, "INTRATE(39493,39583,1000000,1014420,2)", 0.05768, 1e-6);
                SS.Util.Utils.AssertDouble(fe, c, "RECEIVED(39493,39583,1000000,0.0575,2)", 1014584.654, 0.001);
                SS.Util.Utils.AssertDouble(fe, c, "ACCRINTM(39539,39614,0.1,1000,3)", 20.54794521, 1e-6);
                SS.Util.Utils.AssertDouble(fe, c, "TBILLPRICE(39538,39600,0.09)", 98.45, 1e-9);
                SS.Util.Utils.AssertDouble(fe, c, "TBILLYIELD(39538,39600,98.45)", 0.091417, 1e-6);
                SS.Util.Utils.AssertDouble(fe, c, "TBILLEQ(39538,39600,0.0914)", 0.094151, 1e-6);
                SS.Util.Utils.AssertError(fe, c, "TBILLPRICE(39600,39538,0.09)", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "DISC(39107,39248,97.975,100,9)", FormulaError.NUM);
                SS.Util.Utils.AssertDouble(fe, c, "FVSCHEDULE(1,A3:C3)", 1.33089, 1e-9);
            });
        }

        [Test]
        public void TestCumulativeAndIrr()
        {
            Run((fe, c) =>
            {
                SS.Util.Utils.AssertDouble(fe, c, "CUMIPMT(0.09/12,30*12,125000,13,24,0)", -11135.23213, 1e-4);
                SS.Util.Utils.AssertDouble(fe, c, "CUMIPMT(0.09/12,30*12,125000,1,1,0)", -937.5, 1e-6);
                SS.Util.Utils.AssertDouble(fe, c, "CUMIPMT(0.09/12,30*12,125000,1,1,1)", 0, 1e-9);
                SS.Util.Utils.AssertDouble(fe, c, "CUMIPMT(0.09/12,30*12,125000,2,2,1)", -(125000 - 125000*0.0075/(1-System.Math.Pow(1.0075, -360))/1.0075)*0.0075, 1e-6);
                SS.Util.Utils.AssertDouble(fe, c, "CUMPRINC(0.09/12,30*12,125000,13,24,0)", -934.1071234, 1e-4);
                SS.Util.Utils.AssertError(fe, c, "CUMIPMT(0.09/12,360,125000,0,24,0)", FormulaError.NUM);
                SS.Util.Utils.AssertDouble(fe, c, "XNPV(0.09,A1:E1,A2:E2)", 2086.647602, 1e-5);
                SS.Util.Utils.AssertDouble(fe, c, "XIRR(A1:E1,A2:E2,0.1)", 0.373362535, 1e-7);
            });
        }

        [Test]
        public void TestCoupons()
        {
            Run((fe, c) =>
            {
                // settlement 2011-01-25 (40568), maturity 2011-11-15 (40862), semiannual, actual/actual
                SS.Util.Utils.AssertDouble(fe, c, "COUPPCD(40568,40862,2,1)", 40497);
                SS.Util.Utils.AssertDouble(fe, c, "COUPNCD(40568,40862,2,1)", 40678);
                SS.Util.Utils.AssertDouble(fe, c, "COUPDAYBS(40568,40862,2,1)", 71);
                SS.Util.Utils.AssertDouble(fe, c, "COUPDAYS(40568,40862,2,1)", 181);
                SS.Util.Utils.AssertDouble(fe, c, "COUPDAYSNC(40568,40862,2,1)", 110);
                // settlement 2007-01-25 (39107), maturity 2008-11-15 (39767)
                SS.Util.Utils.AssertDouble(fe, c, "COUPNUM(39107,39767,2,1)", 4);
                SS.Util.Utils.AssertError(fe, c, "COUPNUM(39107,39767,3,1)", FormulaError.NUM);
                SS.Util.Utils.AssertError(fe, c, "COUPNUM(39767,39107,2,1)", FormulaError.NUM);
            });
        }

        [Test]
        public void TestBonds()
        {
            Run((fe, c) =>
            {
                // settle 2008-02-15 (39493), maturity 2017-11-15 (43054)
                SS.Util.Utils.AssertDouble(fe, c, "PRICE(39493,43054,0.0575,0.065,100,2,0)", 94.63436162, 1e-6);
                // maturity 2016-11-15 (42689)
                SS.Util.Utils.AssertDouble(fe, c, "YIELD(39493,42689,0.0575,95.04287,100,2,0)", 0.065, 1e-6);
                // settle 2008-01-01 (39448), maturity 2016-01-01 (42370)
                SS.Util.Utils.AssertDouble(fe, c, "DURATION(39448,42370,0.08,0.09,2,1)", 5.993775, 1e-5);
                SS.Util.Utils.AssertDouble(fe, c, "MDURATION(39448,42370,0.08,0.09,2,1)", 5.73567, 1e-5);
                SS.Util.Utils.AssertError(fe, c, "PRICE(39493,43054,-0.01,0.065,100,2,0)", FormulaError.NUM);
            });
        }
    }
}