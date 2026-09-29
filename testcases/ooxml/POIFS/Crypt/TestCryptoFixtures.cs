namespace TestCases.POIFS.Crypt
{
    using NPOI;
    using NPOI.HSSF.Record.Crypto;
    using NPOI.OpenXml4Net.OPC;
    using NPOI.POIFS.Crypt;
    using NPOI.SS.UserModel;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System;
    using System.IO;
    using TestCases;

    /// <summary>
    /// Characterization tests: decrypt real fixtures for each legacy/binary scheme
    /// and open signed packages. Unimplemented Dsig paths are tracked as Ignore in TestSignatureInfo.
    /// </summary>
    [TestFixture]
    public class TestCryptoFixtures
    {
        [TearDown]
        public void ResetPassword()
        {
            Biff8EncryptionKey.CurrentUserPassword = null;
        }

        // binary RC4 (password.xls), CryptoAPI RC4 (97_rc4cryptoapi_password.xls)
        [TestCase("password.xls", "password")]
        [TestCase("97_rc4cryptoapi_password.xls", "Password1234_")]
        public void DecryptXlsWithCorrectPassword(string file, string password)
        {
            using Stream s = POIDataSamples.GetSpreadSheetInstance().OpenResourceAsStream(file);
            using IWorkbook wb = WorkbookFactory.Create(s, password);
            ClassicAssert.Greater(wb.NumberOfSheets, 0);
            ClassicAssert.IsNotNull(wb.GetSheetAt(0));
        }

        [TestCase("password.xls")]
        [TestCase("97_rc4cryptoapi_password.xls")]
        public void DecryptXlsWithWrongPasswordFails(string file)
        {
            using Stream s = POIDataSamples.GetSpreadSheetInstance().OpenResourceAsStream(file);
            Assert.Throws<EncryptedDocumentException>(() => WorkbookFactory.Create(s, "definitely-wrong"));
        }

        // Standard (protect.xlsx) and Agile (protected_sha512.xlsx)
        [TestCase("protect.xlsx", 3, 2)]
        [TestCase("protected_sha512.xlsx", 4, 4)]
        public void EncryptionModeOfXlsx(string file, int major, int minor)
        {
            using var fs = new NPOI.POIFS.FileSystem.POIFSFileSystem(POIDataSamples.GetPOIFSInstance().OpenResourceAsStream(file));
            var info = new EncryptionInfo(fs);
            ClassicAssert.AreEqual(major, info.VersionMajor);
            ClassicAssert.AreEqual(minor, info.VersionMinor);
        }

        [TestCase("protect.xlsx")]
        [TestCase("protected_agile.docx")]
        public void DecryptXlsxWithDefaultPasswordOpensPackage(string file)
        {
            using var fs = new NPOI.POIFS.FileSystem.POIFSFileSystem(POIDataSamples.GetPOIFSInstance().OpenResourceAsStream(file));
            Decryptor d = Decryptor.GetInstance(new EncryptionInfo(fs));
            ClassicAssert.IsTrue(d.VerifyPassword(Decryptor.DEFAULT_PASSWORD));
            using var zin = new ICSharpCode.SharpZipLib.Zip.ZipInputStream(d.GetDataStream(fs.Root));
            bool found = false;
            ICSharpCode.SharpZipLib.Zip.ZipEntry e;
            while((e = zin.GetNextEntry()) != null)
            {
                found |= e.Name == "[Content_Types].xml";
            }
            ClassicAssert.IsTrue(found);
        }

        [TestCase("protect.xlsx")]
        [TestCase("protected_agile.docx")]
        public void DecryptXlsxWithWrongPasswordFails(string file)
        {
            using var fs = new NPOI.POIFS.FileSystem.POIFSFileSystem(POIDataSamples.GetPOIFSInstance().OpenResourceAsStream(file));
            Decryptor d = Decryptor.GetInstance(new EncryptionInfo(fs));
            ClassicAssert.IsFalse(d.VerifyPassword("definitely-wrong"));
        }

        [TestCase("hello-world-signed.xlsx")]
        [TestCase("ms-office-2010-signed.xlsx")]
        public void SignedPackageHasSignatureOrigin(string file)
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory,
                TestContext.Parameters[POIDataSamples.TEST_PROPERTY], "xmldsign", file);
            using OPCPackage pkg = OPCPackage.Open(path, PackageAccess.READ);
            var rels = pkg.GetRelationshipsByType("http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/origin");
            ClassicAssert.AreEqual(1, rels.Size);
        }
    }
}