namespace TestCases.POIFS.Crypt
{
    using NPOI.POIFS.Crypt;
    using NPOI.POIFS.Crypt.CryptoAPI;
    using NPOI.POIFS.FileSystem;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System;
    using System.IO;

    /// <summary>
    /// Covers gaps that used to throw NotImplementedException in Cipher and CryptoAPIDecryptor.
    /// </summary>
    [TestFixture]
    public class TestCryptoGaps
    {
        [Test]
        public void CryptoApiEncryptedSummaryRoundTrip()
        {
            byte[] a = new byte[700];
            byte[] b = new byte[33];
            new Random(1).NextBytes(a);
            new Random(2).NextBytes(b);

            var entries = new NPOIFSFileSystem();
            entries.CreateDocument(new MemoryStream(a), "streamA");
            entries.CreateDocument(new MemoryStream(b), "streamB");

            var info = new EncryptionInfo(EncryptionMode.CryptoAPI);
            Encryptor enc = info.Encryptor;
            enc.ConfirmPassword("pw");
            var target = new NPOIFSFileSystem();
            ((CryptoAPIEncryptor) enc).SetSummaryEntries(target.Root, "EncryptedSummary", entries);

            Decryptor dec = info.Decryptor;
            ClassicAssert.IsTrue(dec.VerifyPassword("pw"));
            var result = new NPOIFSFileSystem(dec.GetDataStream(target.Root));
            AssertStream(a, result.Root, "streamA");
            AssertStream(b, result.Root, "streamB");
        }

        private static void AssertStream(byte[] expected, DirectoryNode dir, string name)
        {
            using DocumentInputStream dis = dir.CreateDocumentInputStream(name);
            byte[] actual = new byte[expected.Length];
            int read = 0;
            while(read < actual.Length)
            {
                int n = dis.Read(actual, read, actual.Length - read);
                if(n <= 0)
                    break;
                read += n;
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void CipherInitWithUnsupportedParameterSpecThrowsArgument()
        {
            Cipher c = Cipher.GetInstance("AES/CBC/NoPadding");
            var key = new SecretKeySpec(new byte[16], "AES");
            Assert.Throws<ArgumentException>(() => c.Init(Cipher.ENCRYPT_MODE, key, new AlgorithmParameterSpec()));
        }

        [Test]
        public void CipherInitWithNullParameterSpecUsesKeyOnly()
        {
            Cipher c = Cipher.GetInstance("RC4");
            var key = new SecretKeySpec(new byte[16], "RC4");
            Assert.DoesNotThrow(() => c.Init(Cipher.ENCRYPT_MODE, key, null));
        }

        [Test]
        public void MaxAllowedKeyLengthUnknownAlgorithmThrowsArgument()
        {
            Assert.Throws<ArgumentException>(() => Cipher.GetMaxAllowedKeyLength("nope"));
        }
    }
}