using NPOI.POIFS.Crypt;
using NPOI.POIFS.FileSystem;
using NPOI.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;

namespace TestCases.POIFS.Crypt
{
    [TestFixture]
    public class TestXorObfuscationMethod2
    {
        private static readonly byte[] ABC_ARRAY =
        {
            0x95, 0x99, 0x94, 0x75, 0xDA, 0x57, 0x78, 0x57, 0xDA, 0x74, 0x65, 0xA8, 0x7A, 0x2F, 0x25, 0x77
        };

        [Test]
        public void TestCreateXorArray2Vectors()
        {
            CollectionAssert.AreEqual(ABC_ARRAY, CryptoFunctions.CreateXorArray2("abc"));

            byte[] expected15 =
            {
                0x2A, 0x58, 0x2B, 0x5B, 0x28, 0x5A, 0x29, 0x5D, 0x2E, 0x59, 0x2A, 0x58, 0x2B, 0x5B, 0x28, 0x9C
            };
            CollectionAssert.AreEqual(expected15, CryptoFunctions.CreateXorArray2("123456789012345"));
            CollectionAssert.AreEqual(expected15, CryptoFunctions.CreateXorArray2("1234567890123456789"));
        }

        [Test]
        public void TestCreateXorArray1Unchanged()
        {
            byte[] expected =
            {
                0xAC, 0xCC, 0xA4, 0xAB, 0xD6, 0xBA, 0xC3, 0xBA, 0xD6, 0xA3, 0x2B, 0x45, 0xD3, 0x79, 0x29, 0xBB
            };
            CollectionAssert.AreEqual(expected, CryptoFunctions.CreateXorArray1("abc"));
        }

        [Test]
        public void TestVerifier2Vectors()
        {
            // abc matches the FilePass of spreadsheet/xor-encryption-abc.xls
            ClassicAssert.AreEqual(0x514ACC1A, CryptoFunctions.CreateXorVerifier2("abc"));
            ClassicAssert.AreEqual(unchecked((int) 0x82659EB1), CryptoFunctions.CreateXorVerifier2("123456789012345"));
        }

        [Test]
        public void TestXorTransform2Vectors()
        {
            byte[] input = { 0x00, 0x41, 0x99, 0x42, 0x75, 0x00, 0x10, 0x20 };

            byte[] data = (byte[])input.Clone();
            CryptoFunctions.XorTransform2(data, 0, data.Length, ABC_ARRAY, 0);
            CollectionAssert.AreEqual(new byte[] { 0x00, 0xD8, 0x0D, 0x37, 0xAF, 0x00, 0x68, 0x77 }, data);

            data = (byte[]) input.Clone();
            CryptoFunctions.XorTransform2(data, 0, data.Length, ABC_ARRAY, 100);
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x16, 0xE1, 0x15, 0xAF, 0x00, 0x75, 0x88 }, data);
        }

        [Test]
        public void TestXorTransform2IsInvolution()
        {
            byte[] original = new byte[4096];
            new Random(1).NextBytes(original);
            byte[] data = (byte[])original.Clone();
            byte[] arr = CryptoFunctions.CreateXorArray2("abc");

            CryptoFunctions.XorTransform2(data, 0, data.Length, arr, 68);
            CryptoFunctions.XorTransform2(data, 0, data.Length, arr, 68);

            CollectionAssert.AreEqual(original, data);
        }

        [Test]
        public void TestXorTransform2ArgumentChecks()
        {
            byte[] data = new byte[8];
            Assert.Throws<ArgumentNullException>(() => CryptoFunctions.XorTransform2(null, 0, 0, ABC_ARRAY, 0));
            Assert.Throws<ArgumentNullException>(() => CryptoFunctions.XorTransform2(data, 0, 0, null, 0));
            Assert.Throws<ArgumentException>(() => CryptoFunctions.XorTransform2(data, 0, 0, new byte[15], 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CryptoFunctions.XorTransform2(data, 4, 5, ABC_ARRAY, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CryptoFunctions.XorTransform2(data, 0, 8, ABC_ARRAY, -1));

            Assert.Throws<ArgumentNullException>(() => CryptoFunctions.CreateXorArray2(null));
            Assert.Throws<ArgumentException>(() => CryptoFunctions.CreateXorArray2(""));
        }

        [Test]
        public void TestDeobfuscateWordFixture()
        {
            POIDataSamples samples = POIDataSamples.GetDocumentInstance();
            using NPOIFSFileSystem obfFs = new NPOIFSFileSystem(samples.ReadFile("xor-obfuscation-abc.doc"));
            using NPOIFSFileSystem plainFs = new NPOIFSFileSystem(samples.ReadFile("xor-obfuscation-abc-plain.doc"));

            byte[] wd = ReadStream(obfFs, "WordDocument");
            byte[] table = ReadStream(obfFs, "1Table");
            byte[] dataStream = ReadStream(obfFs, "Data");

            int flags = LittleEndian.GetUShort(wd, 0x0A);
            ClassicAssert.AreEqual(0x93F0, flags);
            ClassicAssert.AreEqual(0x0100, flags & 0x0100);
            ClassicAssert.AreEqual(0x8000, flags & 0x8000);
            ClassicAssert.AreEqual(CryptoFunctions.CreateXorVerifier2("abc"), LittleEndian.GetInt(wd, 0x0E));

            byte[] arr = CryptoFunctions.CreateXorArray2("abc");
            CryptoFunctions.XorTransform2(wd, 68, wd.Length - 68, arr, 68);
            CryptoFunctions.XorTransform2(table, 0, table.Length, arr, 0);
            CryptoFunctions.XorTransform2(dataStream, 0, dataStream.Length, arr, 0);

            CollectionAssert.AreEqual(ReadStream(plainFs, "1Table"), table);
            CollectionAssert.AreEqual(ReadStream(plainFs, "Data"), dataStream);

            byte[] plainWd = ReadStream(plainFs, "WordDocument");
            ClassicAssert.AreEqual(plainWd.Length, wd.Length);
            for(int i = 0; i < wd.Length; i++)
            {
                if(i == 0x0B || (i >= 0x0E && i <= 0x11))
                    continue;
                ClassicAssert.AreEqual(plainWd[i], wd[i], "WordDocument byte " + i);
            }

            CollectionAssert.AreEqual(
                ReadStream(plainFs, "\u0005SummaryInformation"),
                ReadStream(obfFs, "\u0005SummaryInformation"));
        }

        [Test]
        public void TestWrongPasswordDoesNotVerify()
        {
            ClassicAssert.AreNotEqual(0x514ACC1A, CryptoFunctions.CreateXorVerifier2("wrong"));
        }

        private static byte[] ReadStream(NPOIFSFileSystem fs, string name)
        {
            using DocumentInputStream dis = fs.CreateDocumentInputStream(name);
            byte[] buf = new byte[dis.Available()];
            dis.ReadFully(buf);
            return buf;
        }
    }
}