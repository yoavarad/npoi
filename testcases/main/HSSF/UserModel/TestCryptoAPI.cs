namespace TestCases.HSSF.UserModel
{
    using NPOI.HSSF.Record.Crypto;
    using NPOI.HSSF.UserModel;
    using NPOI.POIFS.FileSystem;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System.IO;

    [TestFixture]
    public class TestCryptoAPI
    {
        [TearDown]
        public void ResetPassword()
        {
            Biff8EncryptionKey.CurrentUserPassword = null;
        }

        [Test]
        public void TestEncryptedSummaryRoundTrip()
        {
            byte[] bytes;
            Biff8EncryptionKey.CurrentUserPassword = "pass";
            using(var wb = new HSSFWorkbook())
            {
                wb.CreateSheet("s").CreateRow(0).CreateCell(0).SetCellValue("x");
                wb.CreateInformationProperties();
                wb.SummaryInformation.Author = "Round Trip Author";
                using var ms = new MemoryStream();
                wb.Write(ms, false);
                bytes = ms.ToArray();
            }

            using(var fs = new NPOIFSFileSystem(new MemoryStream(bytes)))
            {
                ClassicAssert.IsTrue(fs.Root.HasEntryCaseInsensitive("encryption"));
                ClassicAssert.IsFalse(fs.Root.HasEntryCaseInsensitive("\u0005SummaryInformation"));
            }

            using var wb2 = new HSSFWorkbook(new MemoryStream(bytes));
            ClassicAssert.IsNotNull(wb2.SummaryInformation);
            ClassicAssert.AreEqual("Round Trip Author", wb2.SummaryInformation.Author);
        }
    }
}
