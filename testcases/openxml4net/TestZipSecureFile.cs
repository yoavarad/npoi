namespace TestCases.OpenXml4Net.OPC
{
    using ICSharpCode.SharpZipLib.Zip;
    using NPOI.OpenXml4Net.OPC;
    using NPOI.OpenXml4Net.OPC.Internal;
    using NPOI.OpenXml4Net.Util;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System;
    using System.Collections;
    using System.IO;

    /// <summary>Zip-bomb protection (ZipSecureFile limits) enforced on counted bytes.</summary>
    [TestFixture]
    public class TestZipSecureFile
    {
        private double ratio;
        private long maxSize;
        private long maxCount;
        private string tmp;
        private readonly System.Collections.Generic.List<ZipEntrySource> opened = new System.Collections.Generic.List<ZipEntrySource>();

        [SetUp]
        public void SetUp()
        {
            ratio = ZipSecureFile.GetMinInflateRatio();
            maxSize = ZipSecureFile.GetMaxEntrySize();
            maxCount = ZipSecureFile.GetMaxEntryCount();
            tmp = Path.Combine(Path.GetTempPath(), "zsf-" + Guid.NewGuid().ToString("N") + ".zip");
        }

        [TearDown]
        public void TearDown()
        {
            ZipSecureFile.SetMinInflateRatio(ratio);
            ZipSecureFile.SetMaxEntrySize(maxSize);
            ZipSecureFile.SetMaxEntryCount(maxCount);
            foreach(var o in opened) { try { o.Close(); } catch { } }
            opened.Clear();
            if(File.Exists(tmp))
            {
                File.Delete(tmp);
            }
        }

        private static byte[] MakeZip(int entries, int sizeEach, int method, bool zeros = true, bool xml = false)
        {
            using var ms = new MemoryStream();
            using(var zos = new ZipOutputStream(ms) { IsStreamOwner = false })
            {
                zos.SetLevel(9);
                var rnd = new Random(1);
                for(int i = 0; i < entries; i++)
                {
                    var e = new ZipEntry(i == 0 ? "[Content_Types].xml" : "f" + i + ".bin")
                    {
                        CompressionMethod = (CompressionMethod) method
                    };
                    byte[] data = new byte[sizeEach];
                    if(xml && i == 0)
                    {
                        // whitespace-padded but well-formed content types
                        for(int k = 0; k < data.Length; k++) data[k] = (byte) ' ';
                        byte[] open = System.Text.Encoding.ASCII.GetBytes("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
                        byte[] close = System.Text.Encoding.ASCII.GetBytes("</Types>");
                        Array.Copy(open, 0, data, 0, open.Length);
                        Array.Copy(close, 0, data, data.Length - close.Length, close.Length);
                    }
                    else if(!zeros)
                    {
                        rnd.NextBytes(data);
                    }
                    if(method == (int) CompressionMethod.Stored)
                    {
                        e.Size = data.Length;
                        var crc = new ICSharpCode.SharpZipLib.Checksum.Crc32();
                        crc.Update(data);
                        e.Crc = crc.Value;
                    }
                    zos.PutNextEntry(e);
                    zos.Write(data, 0, data.Length);
                    zos.CloseEntry();
                }
            }
            return ms.ToArray();
        }

        private static void DrainAll(ZipEntrySource src)
        {
            IEnumerator en = src.Entries;
            var buf = new byte[8192];
            while(en.MoveNext())
            {
                using Stream s = src.GetInputStream((ZipEntry) en.Current);
                while(s.Read(buf, 0, buf.Length) > 0) { }
            }
        }

        private ZipEntrySource FileSource(byte[] zip)
        {
            foreach(var o in opened) { try { o.Close(); } catch { } }
            opened.Clear();
            File.WriteAllBytes(tmp, zip);
            var src = new ZipFileZipEntrySource(ZipHelper.OpenZipFile(tmp));
            opened.Add(src);
            return src;
        }

        private static ZipEntrySource StreamSource(byte[] zip)
        {
            return new ZipInputStreamZipEntrySource(new ZipInputStream(new MemoryStream(zip)));
        }

        private static void AssertBomb(TestDelegate d)
        {
            var ex = Assert.Throws<ZipSecurityException>(d);
            StringAssert.StartsWith("Zip bomb detected!", ex.Message);
        }

        [Test]
        public void Defaults()
        {
            ClassicAssert.AreEqual(0.01d, ZipSecureFile.GetMinInflateRatio());
            ClassicAssert.AreEqual(0xFFFFFFFFL, ZipSecureFile.GetMaxEntrySize());
            ClassicAssert.AreEqual(10000, ZipSecureFile.GetMaxEntryCount());
            Assert.Throws<ArgumentException>(() => ZipSecureFile.SetMaxEntryCount(-1));
        }

        [Test]
        public void NormalZipPasses()
        {
            byte[] zip = MakeZip(3, 50_000, (int) CompressionMethod.Deflated, zeros: false);
            DrainAll(FileSource(zip));
            DrainAll(StreamSource(zip));
        }

        [Test]
        public void InflateRatio_ZipFile()
        {
            // 20MB of zeros deflates to ~20KB: ratio ~0.001 < 0.01
            byte[] zip = MakeZip(1, 20_000_000, (int) CompressionMethod.Deflated);
            Assert.Less(zip.Length, 100_000);
            AssertBomb(() => DrainAll(FileSource(zip)));
        }

        [Test]
        public void InflateRatio_ZipInputStream()
        {
            byte[] zip = MakeZip(1, 20_000_000, (int) CompressionMethod.Deflated);
            AssertBomb(() => StreamSource(zip));
        }

        [Test]
        public void InflateRatio_IsConfigurable()
        {
            byte[] zip = MakeZip(1, 2_000_000, (int) CompressionMethod.Deflated);
            AssertBomb(() => DrainAll(FileSource(zip)));
            ZipSecureFile.SetMinInflateRatio(0.00001);
            DrainAll(FileSource(zip));
            DrainAll(StreamSource(zip));
        }

        [Test]
        public void MaxEntrySize_Deflated_BothPaths()
        {
            byte[] zip = MakeZip(1, 50_000, (int) CompressionMethod.Deflated, zeros: false);
            ZipSecureFile.SetMaxEntrySize(49_999);
            AssertBomb(() => DrainAll(FileSource(zip)));
            AssertBomb(() => StreamSource(zip));
            ZipSecureFile.SetMaxEntrySize(50_000);
            DrainAll(FileSource(zip));
            DrainAll(StreamSource(zip));
        }

        [Test]
        public void MaxEntrySize_Stored_BothPaths()
        {
            byte[] zip = MakeZip(1, 5_000, (int) CompressionMethod.Stored, zeros: false);
            ZipSecureFile.SetMaxEntrySize(1_000);
            AssertBomb(() => DrainAll(FileSource(zip)));
            AssertBomb(() => StreamSource(zip));
        }

        [Test]
        public void StoredEntryOverGraceSize_IsNotABomb()
        {
            // stored entries have no inflater activity; must not trip the ratio check
            byte[] zip = MakeZip(1, 500_000, (int) CompressionMethod.Stored, zeros: false);
            DrainAll(FileSource(zip));
            DrainAll(StreamSource(zip));
        }

        [Test]
        public void MinInflateRatio_IsValidated()
        {
            Assert.Throws<ArgumentException>(() => ZipSecureFile.SetMinInflateRatio(double.NaN));
            Assert.Throws<ArgumentException>(() => ZipSecureFile.SetMinInflateRatio(-0.1));
            Assert.Throws<ArgumentException>(() => ZipSecureFile.SetMinInflateRatio(1.5));
        }

        [Test]
        public void MaxEntryCount_BothPaths()
        {
            byte[] zip = MakeZip(3, 100, (int) CompressionMethod.Deflated, zeros: false);
            ZipSecureFile.SetMaxEntryCount(2);
            AssertBomb(() => FileSource(zip));
            AssertBomb(() => StreamSource(zip));
            ZipSecureFile.SetMaxEntryCount(3);
            DrainAll(FileSource(zip));
            DrainAll(StreamSource(zip));
        }

        [Test]
        public void MaxEntryCount_OpenPackageFromFile_FailsClosed()
        {
            // must not be swallowed by the "fall back to stream processing" path
            File.WriteAllBytes(tmp, MakeZip(3, 100, (int) CompressionMethod.Deflated, zeros: false));
            long len = new FileInfo(tmp).Length;
            ZipSecureFile.SetMaxEntryCount(2);
            AssertBomb(() => OPCPackage.Open(tmp, PackageAccess.READ));
            Assert.AreEqual(len, new FileInfo(tmp).Length);
        }

        [Test]
        public void BombContentTypes_OpenPackage_FailsClosed()
        {
            byte[] zip = MakeZip(1, 20_000_000, (int) CompressionMethod.Deflated, xml: true);
            File.WriteAllBytes(tmp, zip);
            AssertBomb(() => OPCPackage.Open(tmp, PackageAccess.READ));
            AssertBomb(() => OPCPackage.Open(new MemoryStream(zip)));
        }

        private static void AssertChainHasBomb(Exception e)
        {
            Exception e0 = e;
            for(; e != null; e = e.InnerException)
            {
                if(e.Message != null && e.Message.StartsWith("Zip bomb detected!"))
                {
                    return;
                }
            }
            Assert.Fail("no zip bomb exception in chain: " + e0);
        }
    }
}
