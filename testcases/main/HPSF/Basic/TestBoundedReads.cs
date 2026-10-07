using NPOI.HPSF;
using NPOI.HPSF.Wellknown;
using NPOI.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.IO;

namespace TestCases.HPSF.Basic
{
    /// <summary>Bounds checks and type correctness for HPSF reads/writes (malformed input).</summary>
    [TestFixture]
    public class TestBoundedReads
    {
        private static byte[] Header(int sectionCount, int totalLen)
        {
            byte[] b = new byte[totalLen];
            LittleEndian.PutUShort(b, 0, 0xFFFE);
            LittleEndian.PutUShort(b, 2, 0);
            LittleEndian.PutInt(b, 24, sectionCount);
            return b;
        }

        [Test]
        public void IsPropertySetStream_ShortLength_ReturnsFalseWithoutReadingPastLength()
        {
            // valid header bytes present in the array, but the declared length is too short
            byte[] b = Header(1, 100);
            ClassicAssert.IsFalse(PropertySet.IsPropertySetStream(b, 0, 10));
            ClassicAssert.IsFalse(PropertySet.IsPropertySetStream(b, 0, 27));
            ClassicAssert.IsTrue(PropertySet.IsPropertySetStream(b, 0, 28));
        }

        [Test]
        public void IsPropertySetStream_ArrayShorterThanHeader_ReturnsFalse()
        {
            ClassicAssert.IsFalse(PropertySet.IsPropertySetStream(new byte[5], 0, 5));
            ClassicAssert.IsFalse(PropertySet.IsPropertySetStream(new byte[40], 30, 10));
        }

        [Test]
        public void Ctor_HugeSectionCount_ThrowsClearExceptionQuickly()
        {
            byte[] b = Header(int.MaxValue, 64);
            Assert.Throws<IllegalPropertySetDataException>(() => new PropertySet(b, 0, b.Length));
        }

        [Test]
        public void Ctor_SectionOffsetOutsideStream_Throws()
        {
            byte[] b = Header(1, 48);
            LittleEndian.PutInt(b, 28 + 16, 0x7FFFFFF0);
            Assert.Throws<IllegalPropertySetDataException>(() => new PropertySet(b, 0, b.Length));
        }

        [Test]
        public void Ctor_LengthBeyondArray_Throws()
        {
            byte[] b = Header(1, 48);
            Assert.Throws<NoPropertySetStreamException>(() => new PropertySet(b, 0, 1000));
        }

        [Test]
        public void Ctor_HugePropertyCount_Throws()
        {
            byte[] b = Header(1, 64);
            LittleEndian.PutInt(b, 28 + 16, 48);      // section offset
            LittleEndian.PutInt(b, 48, 16);            // section size
            LittleEndian.PutInt(b, 52, int.MaxValue);  // property count
            Assert.Throws<IllegalPropertySetDataException>(() => new PropertySet(b, 0, b.Length));
        }

        [Test]
        public void Ctor_DuplicateSectionOffsets_Throws()
        {
            byte[] b = Header(2, 100);
            LittleEndian.PutInt(b, 28 + 16, 68);
            LittleEndian.PutInt(b, 28 + 20 + 16, 68);
            Assert.Throws<IllegalPropertySetDataException>(() => new PropertySet(b, 0, b.Length));
        }

        [Test]
        public void TypeWriter_WritesFullUnsignedShortRange()
        {
#pragma warning disable CS0612
            MemoryStream ms = new MemoryStream();
            ClassicAssert.AreEqual(2, TypeWriter.WriteToStream(ms, (ushort) 0xFFFE));
            CollectionAssert.AreEqual(new byte[] { 0xFE, 0xFF }, ms.ToArray());
#pragma warning restore CS0612
        }

        [Test]
        public void Thumbnail_UsesClipboardVariantType()
        {
            SummaryInformation si = PropertySetFactory.CreateSummaryInformation();
            byte[] thumb = new byte[] { 1, 2, 3, 4, 5 };
            si.Thumbnail = thumb;
            ClassicAssert.AreEqual((long) Variant.VT_CF, GetType(si));
        }

        private static long GetType(SummaryInformation si)
        {
            foreach(Property p in si.FirstSection.Properties)
            {
                if(p.ID == PropertyIDMap.PID_THUMBNAIL)
                {
                    return p.Type;
                }
            }
            return -1;
        }
    }
}