using NPOI.SS.Formula.Eval;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace TestCases.SS.Formula.Eval
{
    /// <summary>
    /// NumberEval's text form follows Excel's 15-significant-digit display (e.g. =A1&amp;"").
    /// </summary>
    [TestFixture]
    public class TestNumberEval
    {
        [TestCase(0.1 + 0.2, "0.3")]
        [TestCase(1.0 / 3, "0.333333333333333")]
        [TestCase(123456789012345678d, "123456789012346000")]
        [TestCase(1e21, "1E+21")]
        [TestCase(1234567890.12345678, "1234567890.12346")]
        [TestCase(100d, "100")]
        [TestCase(-0.5, "-0.5")]
        public void TestStringValueUses15SignificantDigits(double value, string expected)
        {
            ClassicAssert.AreEqual(expected, new NumberEval(value).StringValue);
        }
    }
}