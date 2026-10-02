/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

namespace TestCases.HPSF.Basic
{
    using NPOI.HPSF;
    using NPOI.HPSF.Wellknown;
    using NPOI.POIFS.FileSystem;
    using NPOI.Util;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Tests for HPSF gaps that used to throw NotImplementedException on read paths,
    /// plus codepage round-trips for non-Latin text.
    /// </summary>
    [TestFixture]
    public class TestPropertySetGaps
    {
        private static readonly POIDataSamples _samples = POIDataSamples.GetHPSFInstance();

        private static T ReadStream<T>(string file, string streamName) where T : PropertySet
        {
            using Stream inp = _samples.OpenResourceAsStream(file);
            POIFSFileSystem fs = new POIFSFileSystem(inp);
            try
            {
                using DocumentInputStream dis = fs.CreateDocumentInputStream(streamName);
                return (T) PropertySetFactory.Create(dis);
            }
            finally
            {
                fs.Close();
            }
        }

        private static T RoundTrip<T>(T ps) where T : PropertySet
        {
            using MemoryStream bos = new MemoryStream();
            ps.Write(bos);
            return (T) PropertySetFactory.Create(new ByteArrayInputStream(bos.ToArray()));
        }

        [Test]
        public void HeadingPairAndDocpartsAreReadable()
        {
            DocumentSummaryInformation dsi = ReadStream<DocumentSummaryInformation>(
                "TestBug44375.xls", DocumentSummaryInformation.DEFAULT_STREAM_NAME);

            byte[] headingPair = dsi.HeadingPair;
            byte[] docparts = dsi.Docparts;

            ClassicAssert.IsNotNull(headingPair);
            ClassicAssert.IsNotNull(docparts);
            // Both are vectors: the first 4 bytes are the element count.
            ClassicAssert.Greater(LittleEndian.GetInt(headingPair, 0), 0);
            ClassicAssert.Greater(LittleEndian.GetInt(docparts, 0), 0);
        }

        [Test]
        public void HeadingPairAndDocpartsAreNullWhenAbsent()
        {
            DocumentSummaryInformation dsi = PropertySetFactory.NewDocumentSummaryInformation();
            ClassicAssert.IsNull(dsi.HeadingPair);
            ClassicAssert.IsNull(dsi.Docparts);
        }

        [Test]
        public void HeadingPairAndDocpartsRoundTrip()
        {
            DocumentSummaryInformation src = ReadStream<DocumentSummaryInformation>(
                "TestBug44375.xls", DocumentSummaryInformation.DEFAULT_STREAM_NAME);

            DocumentSummaryInformation dsi = PropertySetFactory.NewDocumentSummaryInformation();
            dsi.HeadingPair = src.HeadingPair;
            dsi.Docparts = src.Docparts;

            DocumentSummaryInformation back = RoundTrip(dsi);
            AssertSameVector(src.HeadingPair, back.HeadingPair);
            AssertSameVector(src.Docparts, back.Docparts);

            // Also check the type is kept (VT_VECTOR | VT_LPSTR for this file's doc parts).
            ClassicAssert.AreEqual(TypeOf(src, PropertyIDMap.PID_DOCPARTS), TypeOf(back, PropertyIDMap.PID_DOCPARTS));
            ClassicAssert.AreEqual(TypeOf(src, PropertyIDMap.PID_HEADINGPAIR), TypeOf(back, PropertyIDMap.PID_HEADINGPAIR));

            DocumentSummaryInformation cleared = PropertySetFactory.NewDocumentSummaryInformation();
            cleared.Docparts = src.Docparts;
            cleared.Docparts = null;
            ClassicAssert.IsNull(RoundTrip(cleared).Docparts);
        }

        [Test]
        public void DocpartsSetterKeepsExistingVariantType()
        {
            // count = 1, one VT_LPWSTR element "A": char count 2, "A\0" in UTF-16LE
            byte[] raw = { 1, 0, 0, 0, 2, 0, 0, 0, 0x41, 0, 0, 0 };
            DocumentSummaryInformation dsi = PropertySetFactory.NewDocumentSummaryInformation();
            dsi.FirstSection.SetProperty((int) PropertyIDMap.PID_DOCPARTS, Variant.VT_VECTOR | Variant.VT_LPWSTR, raw);

            dsi.Docparts = raw;
            DocumentSummaryInformation back = RoundTrip(dsi);

            ClassicAssert.AreEqual(Variant.VT_VECTOR | Variant.VT_LPWSTR, TypeOf(back, PropertyIDMap.PID_DOCPARTS));
            AssertSameVector(raw, back.Docparts);
        }

        // Raw vector bytes may carry up to 3 trailing zero bytes of alignment
        // padding, depending on where the value sat in the stream.
        private static void AssertSameVector(byte[] expected, byte[] actual)
        {
            ClassicAssert.IsNotNull(actual);
            ClassicAssert.GreaterOrEqual(actual.Length, expected.Length);
            ClassicAssert.Less(actual.Length - expected.Length, 4);
            for(int i = 0; i < actual.Length; i++)
            {
                ClassicAssert.AreEqual(i < expected.Length ? expected[i] : (byte) 0, actual[i], "byte " + i);
            }
        }

        private static long TypeOf(PropertySet ps, long id)
        {
            foreach(Property p in ps.FirstSection.Properties)
            {
                if(p.ID == id)
                {
                    return p.Type;
                }
            }
            return -1;
        }

        [Test]
        public void PropertySetHashCodeIsConsistentWithEquals()
        {
            SummaryInformation a = ReadStream<SummaryInformation>(
                "TestShiftJIS.doc", SummaryInformation.DEFAULT_STREAM_NAME);
            SummaryInformation b = ReadStream<SummaryInformation>(
                "TestShiftJIS.doc", SummaryInformation.DEFAULT_STREAM_NAME);

            ClassicAssert.AreEqual(a, b);
            ClassicAssert.AreEqual(a.GetHashCode(), b.GetHashCode());

            HashSet<PropertySet> set = new HashSet<PropertySet> { a, b };
            ClassicAssert.AreEqual(1, set.Count);
        }

        [Test]
        public void PropertyIDMapContainsAndCopyTo()
        {
            ICollection<KeyValuePair<long, string>> coll = PropertyIDMap.SummaryInformationProperties;
            KeyValuePair<long, string> title = new KeyValuePair<long, string>(PropertyIDMap.PID_TITLE, "PID_TITLE");

            ClassicAssert.IsTrue(coll.Contains(title));
            ClassicAssert.IsFalse(coll.Contains(new KeyValuePair<long, string>(PropertyIDMap.PID_TITLE, "other")));
            ClassicAssert.IsFalse(coll.Contains(new KeyValuePair<long, string>(-42, "PID_TITLE")));

            KeyValuePair<long, string>[] arr = new KeyValuePair<long, string>[coll.Count + 1];
            coll.CopyTo(arr, 1);
            ClassicAssert.AreEqual(default(KeyValuePair<long, string>), arr[0]);
            CollectionAssert.Contains(arr, title);
        }

        [Test]
        public void ShiftJISCodepageIsReadable()
        {
            SummaryInformation si = ReadStream<SummaryInformation>(
                "TestShiftJIS.doc", SummaryInformation.DEFAULT_STREAM_NAME);
            ClassicAssert.AreEqual(932, si.FirstSection.Codepage);
            ClassicAssert.AreEqual("第1章", si.Title);
        }

        [TestCase(1251, "Привет мир")] // Cyrillic
        [TestCase(932, "第1章 テスト")] // Shift-JIS
        [TestCase(936, "测试文档")] // GBK
        [TestCase(1253, "Γειά σου")] // Greek
        [TestCase(1255, "שלום")] // Hebrew
        [TestCase(1200, "שלום Привет")] // UTF-16
        public void NonLatinCodepageRoundTrip(int codepage, string text)
        {
            SummaryInformation si = PropertySetFactory.NewSummaryInformation();
            si.FirstSection.Codepage = codepage;
            si.Title = text;
            si.Author = text;

            DocumentSummaryInformation dsi = PropertySetFactory.NewDocumentSummaryInformation();
            dsi.FirstSection.Codepage = codepage;
            dsi.Company = text;
            CustomProperties cps = new CustomProperties();
            cps.SetCodepage(codepage);
            cps.Put(text, text);
            dsi.CustomProperties = cps;

            SummaryInformation si2 = RoundTrip(si);
            ClassicAssert.AreEqual(codepage, si2.FirstSection.Codepage);
            ClassicAssert.AreEqual(text, si2.Title);
            ClassicAssert.AreEqual(text, si2.Author);

            DocumentSummaryInformation dsi2 = RoundTrip(dsi);
            ClassicAssert.AreEqual(text, dsi2.Company);
            CustomProperties cps2 = dsi2.CustomProperties;
            ClassicAssert.IsNotNull(cps2);
            ClassicAssert.AreEqual(text, cps2.Get(text));
        }

        [Test]
        public void DocumentStreamReadMatchesByteArrayRead()
        {
            using Stream inp = _samples.OpenResourceAsStream("TestBug44375.xls");
            POIFSFileSystem fs = new POIFSFileSystem(inp);
            try
            {
                string name = DocumentSummaryInformation.DEFAULT_STREAM_NAME;
                byte[] raw;
                using(DocumentInputStream d1 = fs.CreateDocumentInputStream(name))
                {
                    raw = new byte[d1.Available()];
                    d1.ReadFully(raw);
                }
                PropertySet fromBytes = new PropertySet(raw);
                PropertySet fromStream;
                using(DocumentInputStream d2 = fs.CreateDocumentInputStream(name))
                {
                    fromStream = new PropertySet(d2);
                    ClassicAssert.AreEqual(0, d2.Available());
                }
                ClassicAssert.AreEqual(fromBytes.SectionCount, fromStream.SectionCount);
            }
            finally
            {
                fs.Close();
            }
        }
    }
}