namespace TestCases.POIFS.Crypt
{
    using NPOI;
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

        [Test]
        public void CryptoApiEncryptedSummaryCustomStreamName()
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
            ((CryptoAPIEncryptor) enc).SetSummaryEntries(target.Root, "encryption", entries);

            var dec = (CryptoAPIDecryptor) info.Decryptor;
            ClassicAssert.IsTrue(dec.VerifyPassword("pw"));
            var result = dec.GetSummaryEntries(target.Root, "encryption");
            AssertStream(a, result.Root, "streamA");
            AssertStream(b, result.Root, "streamB");
        }

        [Test]
        public void GetSummaryEntriesMissingStreamThrows()
        {
            var info = new EncryptionInfo(EncryptionMode.CryptoAPI);
            info.Encryptor.ConfirmPassword("pw");
            var dec = (CryptoAPIDecryptor) info.Decryptor;
            ClassicAssert.IsTrue(dec.VerifyPassword("pw"));
            var target = new NPOIFSFileSystem();
            Assert.Throws<FileNotFoundException>(() => dec.GetSummaryEntries(target.Root, "encryption"));
        }

        [TestCase(0x7FFFFFFFu)]
        [TestCase(0xFFFFFFFFu)]
        [TestCase(1000000u)]
        public void GetSummaryEntriesRejectsHugeDescriptorCount(uint count)
        {
            var (dec, root) = BuildTampered(data =>
            {
                // single 3-byte stream: descriptor array at offset 11, count at [11..14]
                Flip(data, 11, 1u ^ count);
            });
            Assert.Throws<EncryptedDocumentException>(() => dec.GetSummaryEntries(root, "encryption"));
        }

        [TestCase(0u)]
        [TestCase(100u)]
        [TestCase(0xFFFFFFFFu)]
        public void GetSummaryEntriesRejectsBadArrayOffset(uint newOffset)
        {
            var (dec, root) = BuildTampered(data => Flip(data, 0, 11u ^ newOffset));
            Assert.Throws<EncryptedDocumentException>(() => dec.GetSummaryEntries(root, "encryption"));
        }

        private static void Flip(byte[] data, int pos, uint mask)
        {
            for(int i = 0; i < 4; i++)
            {
                data[pos + i] ^= (byte) (mask >> (8 * i));
            }
        }

        private static (CryptoAPIDecryptor, DirectoryNode) BuildTampered(Action<byte[]> tamper)
        {
            var entries = new NPOIFSFileSystem();
            entries.CreateDocument(new MemoryStream(new byte[] { 1, 2, 3 }), "s");
            var info = new EncryptionInfo(EncryptionMode.CryptoAPI);
            info.Encryptor.ConfirmPassword("pw");
            var tmp = new NPOIFSFileSystem();
            ((CryptoAPIEncryptor) info.Encryptor).SetSummaryEntries(tmp.Root, "encryption", entries);

            byte[] data;
            using(DocumentInputStream dis = tmp.Root.CreateDocumentInputStream("encryption"))
            {
                data = new byte[dis.Available()];
                dis.ReadFully(data);
            }
            tamper(data);
            var target = new NPOIFSFileSystem();
            target.Root.CreateDocument("encryption", new MemoryStream(data));

            var dec = (CryptoAPIDecryptor) info.Decryptor;
            ClassicAssert.IsTrue(dec.VerifyPassword("pw"));
            return (dec, target.Root);
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