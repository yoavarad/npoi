using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace TestCases.SS.Formula.Atp
{
    /// <summary>
    /// FILTER, SORT, UNIQUE and SEQUENCE, evaluated as legacy (CSE) array formulas over a range of
    /// the result's size. Spilling is covered by TestXSSFDynamicArrays. Examples follow the Excel docs.
    /// </summary>
    [TestFixture]
    public class TestDynamicArrayFunctions
    {
        private HSSFWorkbook _wb;
        private ISheet _sheet;
        private HSSFFormulaEvaluator _fe;

        [SetUp]
        public void SetUp()
        {
            _wb = new HSSFWorkbook();
            _sheet = _wb.CreateSheet();
            // https://support.microsoft.com/en-us/office/filter-function-f4f7cb66-82eb-4767-8f7c-4877ad80c759
            object[][] data =
            {
                new object[] { "Region", "Sales Rep", "Product", "Units" },
                new object[] { "East", "Tom", "Apple", 6380d },
                new object[] { "West", "Fred", "Grape", 5619d },
                new object[] { "North", "Amy", "Pear", 4565d },
                new object[] { "South", "Sal", "Banana", 5323d },
                new object[] { "East", "Fritz", "Apple", 4394d },
                new object[] { "West", "Sravan", "Grape", 7195d },
            };
            for(int r = 0; r < data.Length; r++)
            {
                IRow row = _sheet.CreateRow(r);
                for(int c = 0; c < data[r].Length; c++)
                {
                    if(data[r][c] is string s)
                    {
                        row.CreateCell(c).SetCellValue(s);
                    }
                    else
                    {
                        row.CreateCell(c).SetCellValue((double) data[r][c]);
                    }
                }
            }
            _fe = new HSSFFormulaEvaluator(_wb);
        }

        [TearDown]
        public void TearDown()
        {
            _wb.Close();
        }

        private object[,] EvalArray(string formula, string range)
        {
            CellRangeAddress address = CellRangeAddress.ValueOf(range);
            _sheet.SetArrayFormula(formula, address);
            var result = new object[address.LastRow - address.FirstRow + 1, address.LastColumn - address.FirstColumn + 1];
            for(int r = address.FirstRow; r <= address.LastRow; r++)
            {
                for(int c = address.FirstColumn; c <= address.LastColumn; c++)
                {
                    CellValue cv = _fe.Evaluate(_sheet.GetRow(r).GetCell(c));
                    result[r - address.FirstRow, c - address.FirstColumn] = cv.CellType switch {
                        CellType.Numeric => cv.NumberValue,
                        CellType.String => cv.StringValue,
                        CellType.Boolean => cv.BooleanValue,
                        CellType.Error => FormulaError.ForInt(cv.ErrorValue),
                        _ => null,
                    };
                }
            }
            return result;
        }

        private int _nextScratchRow = 40;

        // evaluates in a one-cell array formula, so range comparisons inside stay arrays as in Excel 365
        private object EvalSingle(string formula)
        {
            return EvalArray(formula, "A" + (++_nextScratchRow))[0, 0];
        }

        private void AssertError(string formula, FormulaError expected)
        {
            ClassicAssert.AreEqual(expected, EvalSingle(formula), formula);
        }

        [Test]
        public void TestSequence()
        {
            // SEQUENCE(4,5): 4 rows by 5 columns of 1..20, filled row by row
            object[,] grid = EvalArray("SEQUENCE(4,5)", "H1:L4");
            for(int r = 0; r < 4; r++)
            {
                for(int c = 0; c < 5; c++)
                {
                    ClassicAssert.AreEqual(r * 5 + c + 1d, grid[r, c]);
                }
            }

            CollectionAssert.AreEqual(new object[,] { { 1d }, { 2d }, { 3d } }, EvalArray("SEQUENCE(3)", "H6:H8"));
            CollectionAssert.AreEqual(new object[,] { { 10d, 15d, 20d } }, EvalArray("SEQUENCE(1,3,10,5)", "H10:J10"));
            CollectionAssert.AreEqual(new object[,] { { 0d, -0.5d } }, EvalArray("SEQUENCE(1,2,0,-0.5)", "H12:I12"));
            CollectionAssert.AreEqual(new object[,] { { 1d }, { 2d } }, EvalArray("SEQUENCE(2.9)", "H14:H15"));
        }

        [Test]
        public void TestSequenceErrors()
        {
            AssertError("SEQUENCE(0)", FormulaError.CALC);
            AssertError("SEQUENCE(2,0)", FormulaError.CALC);
            AssertError("SEQUENCE(-1)", FormulaError.VALUE);
            AssertError("SEQUENCE(\"x\")", FormulaError.VALUE);
            AssertError("SEQUENCE(1/0)", FormulaError.DIV0);
            // a blank reference counts as 0 rows
            AssertError("SEQUENCE(Z99)", FormulaError.CALC);
            AssertError("SEQUENCE(100000,1000)", FormulaError.NUM);
        }

        [Test]
        public void TestFilterRows()
        {
            object[,] apples = EvalArray("FILTER(A2:D7,C2:C7=\"Apple\")", "H1:K2");
            CollectionAssert.AreEqual(new object[,]
            {
                { "East", "Tom", "Apple", 6380d },
                { "East", "Fritz", "Apple", 4394d },
            }, apples);

            // multiple criteria, AND-ed by multiplication
            object[,] eastGrapes = EvalArray("FILTER(A2:D7,(A2:A7=\"West\")*(D2:D7>6000))", "H4:K4");
            CollectionAssert.AreEqual(new object[,] { { "West", "Sravan", "Grape", 7195d } }, eastGrapes);

            // one column only
            CollectionAssert.AreEqual(new object[,] { { "Amy" }, { "Sal" } },
                EvalArray("FILTER(B2:B7,(D2:D7>4500)*(D2:D7<5500))", "H6:H7"));
        }

        [Test]
        public void TestFilterColumns()
        {
            CollectionAssert.AreEqual(new object[,] { { "Region", "Product" } }, EvalArray("FILTER(A1:D1,{1,0,1,0})", "H1:I1"));
            CollectionAssert.AreEqual(new object[,] { { "Sales Rep", "Units" } }, EvalArray("FILTER(A1:D1,{FALSE,TRUE,FALSE,TRUE})", "H2:I2"));
        }

        [Test]
        public void TestFilterEmptyAndErrors()
        {
            AssertError("FILTER(A2:D7,C2:C7=\"Kiwi\")", FormulaError.CALC);
            ClassicAssert.AreEqual("none", EvalSingle("FILTER(A2:D7,C2:C7=\"Kiwi\",\"none\")"));
            // include must match the array's height (rows) or width (columns)
            AssertError("FILTER(A2:D7,C2:C6=\"Apple\")", FormulaError.VALUE);
            AssertError("FILTER(A2:D7,{\"a\";\"b\";\"c\";\"d\";\"e\";\"f\"})", FormulaError.VALUE);
            AssertError("FILTER(A2:D7,{1;1;#N/A;0;0;0})", FormulaError.NA);
        }

        [Test]
        public void TestSort()
        {
            // default: ascending by first column
            CollectionAssert.AreEqual(new object[,] { { "Amy" }, { "Fred" }, { "Fritz" }, { "Sal" }, { "Sravan" }, { "Tom" } },
                EvalArray("SORT(B2:B7)", "H1:H6"));

            // by units, descending
            object[,] byUnits = EvalArray("SORT(B2:D7,3,-1)", "H8:J13");
            CollectionAssert.AreEqual(new object[] { 7195d, 6380d, 5619d, 5323d, 4565d, 4394d },
                new[] { byUnits[0, 2], byUnits[1, 2], byUnits[2, 2], byUnits[3, 2], byUnits[4, 2], byUnits[5, 2] });
            ClassicAssert.AreEqual("Sravan", byUnits[0, 0]);

            // two keys: product ascending, then units descending; ties keep source order
            object[,] byProduct = EvalArray("SORT(C2:D7,{1,2},{1,-1})", "H15:I20");
            CollectionAssert.AreEqual(new object[,]
            {
                { "Apple", 6380d }, { "Apple", 4394d }, { "Banana", 5323d },
                { "Grape", 7195d }, { "Grape", 5619d }, { "Pear", 4565d },
            }, byProduct);
        }

        [Test]
        public void TestSortMixedTypesAndByColumn()
        {
            // numbers < text (case-insensitive) < FALSE < TRUE
            CollectionAssert.AreEqual(new object[,] { { 1d }, { 3d }, { "A" }, { "b" }, { false }, { true } },
                EvalArray("SORT({3;\"b\";TRUE;1;\"A\";FALSE})", "H1:H6"));
            CollectionAssert.AreEqual(new object[,] { { true }, { false }, { "b" }, { "A" }, { 3d }, { 1d } },
                EvalArray("SORT({3;\"b\";TRUE;1;\"A\";FALSE},1,-1)", "I1:I6"));
            CollectionAssert.AreEqual(new object[,] { { 1d, 2d, 3d }, { "x", "y", "z" } },
                EvalArray("SORT({3,1,2;\"z\",\"x\",\"y\"},1,1,TRUE)", "H8:J9"));
        }

        [Test]
        public void TestSortErrors()
        {
            AssertError("SORT(A2:D7,5)", FormulaError.VALUE);
            AssertError("SORT(A2:D7,0)", FormulaError.VALUE);
            AssertError("SORT(A2:D7,1,2)", FormulaError.VALUE);
        }

        [Test]
        public void TestUnique()
        {
            // text compares case-insensitively; first occurrence wins
            CollectionAssert.AreEqual(new object[,] { { "a" }, { "b" }, { "c" } },
                EvalArray("UNIQUE({\"a\";\"b\";\"A\";\"c\";\"b\"})", "H1:H3"));
            CollectionAssert.AreEqual(new object[,] { { "East" }, { "West" }, { "North" }, { "South" } },
                EvalArray("UNIQUE(A2:A7)", "I1:I4"));
            // whole rows
            CollectionAssert.AreEqual(new object[,] { { "East", "Apple" }, { "West", "Grape" }, { "North", "Pear" }, { "South", "Banana" } },
                EvalArray("UNIQUE(FILTER(A2:C7,{1,0,1}))", "J1:K4"));
            // exactly_once
            CollectionAssert.AreEqual(new object[,] { { "North" }, { "South" } }, EvalArray("UNIQUE(A2:A7,FALSE,TRUE)", "M1:M2"));
            // by column
            CollectionAssert.AreEqual(new object[,] { { 1d, 2d, 3d } }, EvalArray("UNIQUE({1,2,1,3,2},TRUE)", "H6:J6"));
        }

        [Test]
        public void TestUniqueErrors()
        {
            AssertError("UNIQUE({1;1},FALSE,TRUE)", FormulaError.CALC);
            AssertError("UNIQUE(#REF!)", FormulaError.REF);
        }

        [Test]
        public void TestCombined()
        {
            // the Excel docs' SORT(UNIQUE(...)) and SORT(FILTER(...)) patterns
            CollectionAssert.AreEqual(new object[,] { { "East" }, { "North" }, { "South" }, { "West" } },
                EvalArray("SORT(UNIQUE(A2:A7))", "H1:H4"));
            CollectionAssert.AreEqual(new object[,] { { "Fritz" }, { "Tom" } },
                EvalArray("SORT(FILTER(B2:B7,A2:A7=\"East\"))", "I1:I2"));
            ICell cell = _sheet.CreateRow(30).CreateCell(0);
            TestCases.SS.Util.Utils.AssertDouble(_fe, cell, "SUM(SEQUENCE(10))", 55);
            TestCases.SS.Util.Utils.AssertDouble(_fe, cell, "ROWS(UNIQUE(A2:A7))", 4);
        }
    }
}