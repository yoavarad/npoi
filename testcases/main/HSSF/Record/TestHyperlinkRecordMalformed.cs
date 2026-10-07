namespace TestCases.HSSF.Record
{
    using System;
    using System.IO;
    using NPOI.HSSF.Record;
    using NPOI.Util;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;

    /// <summary>
    /// Malformed (untrusted) HLINK records must fail fast with RecordFormatException,
    /// never hang, over-allocate or throw unrelated exceptions.
    /// </summary>
    [TestFixture]
    public class TestHyperlinkRecordMalformed
    {
        private static readonly byte[] Guid1 = [
            0xD0, 0xC9, 0xEA, 0x79, 0xF9, 0xBA, 0xCE, 0x11, 0x8C, 0x82, 0x00, 0xAA, 0x00, 0x4B, 0xA9, 0x0B];
        private static readonly byte[] UrlMoniker = [
            0xE0, 0xC9, 0xEA, 0x79, 0xF9, 0xBA, 0xCE, 0x11, 0x8C, 0x82, 0x00, 0xAA, 0x00, 0x4B, 0xA9, 0x0B];
        private static readonly byte[] FileMoniker = [
            0x03, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46];

        private static byte[] Build(int opts, byte[] moniker, params int[] ints)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(new byte[8]);
            w.Write(Guid1);
            w.Write(2);
            w.Write(opts);
            w.Write(moniker);
            foreach(int i in ints)
            {
                w.Write(i);
            }
            w.Write(new byte[16]);
            w.Flush();
            return ms.ToArray();
        }

        private static void AssertRejected(byte[] data)
        {
            RecordInputStream in1 = TestcaseRecordInputStream.Create(HyperlinkRecord.sid, data);
            Assert.Throws<RecordFormatException>(() => new HyperlinkRecord(in1));
        }

        [TestCase(0)]
        [TestCase(4)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        [TestCase(0x7FFFFFF0)]
        public void UrlMonikerBadLength(int length)
        {
            // HLINK_URL | HLINK_ABS
            AssertRejected(Build(0x03, UrlMoniker, length));
        }

        [TestCase(-1)]
        [TestCase(int.MinValue)]
        [TestCase(0x7FFFFFF0)]
        public void LabelBadLength(int length)
        {
            // HLINK_URL | HLINK_LABEL
            AssertRejected(Build(0x15, UrlMoniker, length));
        }

        [TestCase(-1)]
        [TestCase(0x7FFFFFF0)]
        public void UncPathBadLength(int length)
        {
            // HLINK_URL | HLINK_UNC_PATH
            RecordInputStream in1 = TestcaseRecordInputStream.Create(HyperlinkRecord.sid,
                BuildUnc(length));
            Assert.Throws<RecordFormatException>(() => new HyperlinkRecord(in1));
        }

        private static byte[] BuildUnc(int length)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(new byte[8]);
            w.Write(Guid1);
            w.Write(2);
            w.Write(0x103);
            w.Write(length);
            w.Write(new byte[16]);
            w.Flush();
            return ms.ToArray();
        }

        [TestCase(-1)]
        [TestCase(0x7FFFFFF0)]
        public void FileMonikerBadLength(int length)
        {
            // HLINK_URL | HLINK_ABS ; file moniker: short opts, int len
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(new byte[8]);
            w.Write(Guid1);
            w.Write(2);
            w.Write(0x03);
            w.Write(FileMoniker);
            w.Write((short) 0);
            w.Write(length);
            w.Write(new byte[16]);
            w.Flush();
            AssertRejected(ms.ToArray());
        }
    }
}