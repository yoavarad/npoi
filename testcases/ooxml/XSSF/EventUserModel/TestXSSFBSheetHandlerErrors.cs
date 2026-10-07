using NPOI.SS.UserModel;
using NPOI.XSSF.Binary;
using NPOI.XSSF.EventUserModel;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Collections.Generic;
using System.IO;

namespace TestCases.XSSF.EventUserModel
{
    [TestFixture]
    public class TestXSSFBSheetHandlerErrors
    {
        private sealed class Capture : XSSFSheetXMLHandler.ISheetContentsHandler
        {
            public readonly List<string> Values = new List<string>();
            public void StartRow(int rowNum) { }
            public void EndRow(int rowNum) { }
            public void Cell(string cellReference, string formattedValue, XSSFComment comment) => Values.Add(formattedValue);
            public void HeaderFooter(string text, bool isHeader, string tagName) { }
            public void EndSheet() { }
        }

        private static string Handle(XSSFBRecordType type, byte errorCode)
        {
            var capture = new Capture();
            var handler = new XSSFBSheetHandler(Stream.Null, null, null, null, capture, new DataFormatter(), false);
            // cell header: col(4) + style(3) + flags(1), then the error byte
            handler.HandleRecord((int) type, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, errorCode });
            ClassicAssert.AreEqual(1, capture.Values.Count);
            return capture.Values[0];
        }

        [TestCase((byte) 0x00, "#NULL!")]
        [TestCase((byte) 0x07, "#DIV/0!")]
        [TestCase((byte) 0x0F, "#VALUE!")]
        [TestCase((byte) 0x17, "#REF!")]
        [TestCase((byte) 0x1D, "#NAME?")]
        [TestCase((byte) 0x24, "#NUM!")]
        [TestCase((byte) 0x2A, "#N/A")]
        public void CellErrorTypeIsDecoded(byte code, string expected)
        {
            ClassicAssert.AreEqual(expected, Handle(XSSFBRecordType.BrtCellError, code));
        }

        [Test]
        public void FormulaErrorTypeIsDecoded()
        {
            ClassicAssert.AreEqual("#DIV/0!", Handle(XSSFBRecordType.BrtFmlaError, 0x07));
        }

        [Test]
        public void UnknownErrorCodeFallsBack()
        {
            ClassicAssert.AreEqual("ERROR", Handle(XSSFBRecordType.BrtCellError, 0x55));
        }
    }
}