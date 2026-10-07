namespace TestCases.HSSF.Record
{
    using System.IO;
    using NPOI.HSSF.Record;
    using NPOI.Util;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;

    [TestFixture]
    public class TestRecordGaps108
    {
        private static byte[] BuildCf12(int extLen, byte[] extTail)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write((short) 0x087A); // FtrHeader record type
            w.Write((short) 0);
            w.Write(new byte[8]);     // associated range
            w.Write((byte) 2);        // condition type: formula
            w.Write((byte) 0);        // comparison op
            w.Write((short) 0);       // formula1 len
            w.Write((short) 0);       // formula2 len
            w.Write(extLen);
            if(extLen != 0)
            {
                w.Write(0);           // formatting options: no font/border/pattern blocks
                w.Write((short) 0);
                w.Write(extTail);
            }
            else
            {
                w.Write((short) 0);
            }
            w.Write((short) 0);       // formula_scale len
            w.Write((byte) 0);        // ext_opts
            w.Write((short) 1);       // priority
            w.Write((short) 0);       // template type
            w.Write((byte) 0);        // template param length
            w.Flush();
            return ms.ToArray();
        }

        private static CFRule12Record Parse(byte[] data)
        {
            return new CFRule12Record(TestcaseRecordInputStream.Create(CFRule12Record.sid, data));
        }

        [Test]
        public void Cf12ExtendedFormattingRoundTripsAndClones()
        {
            byte[] data = BuildCf12(6 + 3, [1, 2, 3]);
            CFRule12Record rec = Parse(data);
            ClassicAssert.AreEqual(data.Length + 4, rec.Serialize().Length);
            byte[] ser = rec.Serialize();
            byte[] cloned = ((CFRule12Record) rec.Clone()).Serialize();
            CollectionAssert.AreEqual(ser, cloned);
        }

        [Test]
        public void Cf12NegativeExtendedLengthRejected()
        {
            byte[] data = BuildCf12(-5, []);
            Assert.Throws<RecordFormatException>(() => Parse(data));
        }

        [Test]
        public void Cf12HugeExtendedLengthRejected()
        {
            byte[] data = BuildCf12(0x7FFFFFF0, []);
            Assert.Throws<RecordFormatException>(() => Parse(data));
        }

        [Test]
        public void EmbeddedObjectRefCloneIsIndependent()
        {
            var rec = new EmbeddedObjectRefSubRecord();
            var clone = (EmbeddedObjectRefSubRecord) rec.Clone();
            ClassicAssert.AreNotSame(rec, clone);
            CollectionAssert.AreEqual(rec.Serialize(), clone.Serialize());
            clone.SetUnknownFormulaData([0x02, 0x01, 0x01, 0x16, 0x09]);
            CollectionAssert.AreNotEqual(rec.Serialize(), clone.Serialize());
        }
    }
}
