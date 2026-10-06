using NPOI.HPSF.Extractor;
using NPOI.HSSF.Extractor;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.POIFS.NIO;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.IO;

namespace TestCases
{
    /// <summary>
    /// Task #39: file-backed objects must release their file handle once
    /// closed/disposed, so the file can be deleted straight away (Windows
    /// refuses to delete a file with an open handle).
    /// </summary>
    [TestFixture]
    public class TestFileHandleRelease
    {
        private static FileInfo TempCopy(POIDataSamples samples, string name)
        {
            string path = Path.Combine(Path.GetTempPath(), "npoi-t39-" + Guid.NewGuid().ToString("N") + ".bin");
            using(FileStream src = samples.GetFile(name))
            using(FileStream dst = File.Create(path))
            {
                src.CopyTo(dst);
            }
            return new FileInfo(path);
        }

        private static void AssertDeletable(FileInfo file)
        {
            // FileShare.None fails on any platform while a handle is still open (File.Delete only fails on Windows)
            using(new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
            }
            File.Delete(file.FullName);
            ClassicAssert.IsFalse(File.Exists(file.FullName), "file still exists: " + file.FullName);
        }

        [Test]
        public void FileBackedDataSourceFromFileDoesNotHoldHandle()
        {
            FileInfo tmp = TempCopy(POIDataSamples.GetPOIFSInstance(), "BlockSize512.zvi");
            FileBackedDataSource ds = new FileBackedDataSource(tmp, true);
            try
            {
                ClassicAssert.AreEqual(tmp.Length, ds.Size);
                AssertDeletable(tmp);
            }
            finally
            {
                ds.Close();
            }
        }

        [Test]
        public void NPOIFSFileSystemUsingReleasesFile()
        {
            FileInfo tmp = TempCopy(POIDataSamples.GetPOIFSInstance(), "BlockSize512.zvi");
            using(NPOIFSFileSystem fs = new NPOIFSFileSystem(tmp))
            {
                ClassicAssert.IsTrue(fs.Root.EntryCount > 0);
            }
            AssertDeletable(tmp);
        }

        [Test]
        public void POIFSFileSystemUsingReleasesFile()
        {
            FileInfo tmp = TempCopy(POIDataSamples.GetSpreadSheetInstance(), "SampleSS.xls");
            using(POIFSFileSystem fs = new POIFSFileSystem(tmp))
            {
                ClassicAssert.IsTrue(fs.Root.HasEntry("Workbook"));
            }
            AssertDeletable(tmp);
        }

        [Test]
        public void POIFSFileSystemDisposeIsIdempotent()
        {
            POIFSFileSystem fs = new POIFSFileSystem();
            fs.Dispose();
            Assert.DoesNotThrow(() => fs.Dispose());
            Assert.DoesNotThrow(() => fs.Close());
        }

        [Test]
        public void HSSFWorkbookFromFileReleasesFile()
        {
            FileInfo tmp = TempCopy(POIDataSamples.GetSpreadSheetInstance(), "SampleSS.xls");
            using(POIFSFileSystem fs = new POIFSFileSystem(tmp))
            using(HSSFWorkbook wb = new HSSFWorkbook(fs))
            {
                ClassicAssert.IsTrue(wb.NumberOfSheets > 0);
            }
            AssertDeletable(tmp);
        }

        [Test]
        public void ExtractorsUsingReleasesFile()
        {
            FileInfo tmp = TempCopy(POIDataSamples.GetSpreadSheetInstance(), "SampleSS.xls");
            using(ExcelExtractor extractor = new ExcelExtractor(new POIFSFileSystem(tmp)))
            {
                ClassicAssert.IsNotNull(extractor.Text);
            }
            using(HPSFPropertiesExtractor extractor = new HPSFPropertiesExtractor(new POIFSFileSystem(tmp)))
            {
                ClassicAssert.IsNotNull(extractor.Text);
            }
            AssertDeletable(tmp);
        }

        [Test]
        public void OldExcelExtractorUsingReleasesNonOle2File()
        {
            // testEXCEL_4.xls is a bare BIFF4 stream (not OLE2), so the
            // extractor keeps the FileStream open until it is closed.
            FileInfo tmp = TempCopy(POIDataSamples.GetSpreadSheetInstance(), "testEXCEL_4.xls");
            OldExcelExtractor extractor = new OldExcelExtractor(tmp);
            using(extractor)
            {
                ClassicAssert.IsNotNull(extractor.Text);
            }
            Assert.DoesNotThrow(() => extractor.Dispose());
            Assert.DoesNotThrow(() => extractor.Close());
            AssertDeletable(tmp);
        }
    }
}