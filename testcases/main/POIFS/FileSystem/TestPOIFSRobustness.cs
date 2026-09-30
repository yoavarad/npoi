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

using NPOI.POIFS.FileSystem;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TestCases.POIFS.FileSystem
{
    /// <summary>
    /// Round-trip tests at mini/big stream boundaries, and robustness tests
    /// against malformed CFB input. Malformed input must raise one of the
    /// documented exceptions and never hang:
    /// <list type="bullet">
    /// <item>InvalidOperationException - loop in a sector chain</item>
    /// <item>IndexOutOfRangeException - chain references a sector that does not exist,
    /// or ends before the declared stream size</item>
    /// <item>IOException - malformed header / directory metadata</item>
    /// <item>NPOI.Util.RuntimeException - negative document size (pre-existing behaviour)</item>
    /// </list>
    /// </summary>
    [TestFixture]
    public class TestPOIFSRobustness
    {
        private static readonly POIDataSamples _inst = POIDataSamples.GetPOIFSInstance();
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

        private static readonly int[] BoundarySizes =
        {
            0, 1, 63, 64, 65, 511, 512, 513, 4095, 4096, 4097, 8192, 8193
        };

        private static byte[] Pattern(int n, int seed)
        {
            byte[] b = new byte[n];
            for(int i = 0; i < n; i++)
                b[i] = (byte) (i * 31 + seed);
            return b;
        }

        private static byte[] Write(NPOIFSFileSystem fs)
        {
            MemoryStream ms = new MemoryStream();
            fs.WriteFileSystem(ms);
            return ms.ToArray();
        }

        private static byte[] BuildFile(params (string name, int size)[] docs)
        {
            NPOIFSFileSystem fs = new NPOIFSFileSystem();
            try
            {
                foreach(var d in docs)
                    fs.CreateDocument(new MemoryStream(Pattern(d.size, 7)), d.name);
                return Write(fs);
            }
            finally
            {
                fs.Close();
            }
        }

        private static byte[] ReadDocument(DocumentEntry doc)
        {
            MemoryStream ms = new MemoryStream();
            using(DocumentInputStream dis = new DocumentInputStream(doc))
            {
                byte[] buf = new byte[1000];
                int n;
                while((n = dis.Read(buf, 0, buf.Length)) > 0)
                    ms.Write(buf, 0, n);
            }
            return ms.ToArray();
        }

        private static void Walk(DirectoryEntry dir, string prefix, Dictionary<string, byte[]> result)
        {
            IEnumerator<Entry> it = dir.Entries;
            while(it.MoveNext())
            {
                Entry e = it.Current;
                if(e is DirectoryEntry sub)
                    Walk(sub, prefix + e.Name + "/", result);
                else if(e is DocumentEntry doc)
                    result[prefix + e.Name] = ReadDocument(doc);
            }
        }

        private static Dictionary<string, byte[]> ReadAll(byte[] file)
        {
            Dictionary<string, byte[]> result = new Dictionary<string, byte[]>();
            NPOIFSFileSystem fs = new NPOIFSFileSystem(new MemoryStream(file));
            try
            {
                Walk(fs.Root, "", result);
            }
            finally
            {
                fs.Close();
            }
            return result;
        }

        private static T RunWithTimeout<T>(Func<T> action)
        {
            Task<T> t = Task.Run(action);
            if(!t.Wait(Timeout))
                Assert.Fail("Operation did not complete within " + Timeout + " - possible infinite loop");
            return t.Result;
        }

        // ---- Round trips at stream size boundaries ----

        [Test]
        public void TestRoundTripAtBoundaries512()
        {
            NPOIFSFileSystem fs = new NPOIFSFileSystem();
            foreach(int size in BoundarySizes)
                fs.CreateDocument(new MemoryStream(Pattern(size, size)), "Doc" + size);
            byte[] file = Write(fs);
            fs.Close();

            Dictionary<string, byte[]> read = ReadAll(file);
            foreach(int size in BoundarySizes)
                CollectionAssert.AreEqual(Pattern(size, size), read["Doc" + size], "size " + size);
        }

        [Test]
        public void TestRoundTripAtBoundaries4096()
        {
            // Share read/write: other fixtures may hold this sample open concurrently
            MemoryStream sample = new MemoryStream();
            using(FileStream s = new FileStream(_inst.GetFileInfo("BlockSize4096.zvi").FullName,
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                s.CopyTo(sample);
            }
            sample.Position = 0;
            NPOIFSFileSystem fs = new NPOIFSFileSystem(sample);
            ClassicAssert.AreEqual(4096, fs.GetBigBlockSize());
            Dictionary<string, byte[]> before = new Dictionary<string, byte[]>();
            Walk(fs.Root, "", before);

            foreach(int size in BoundarySizes)
                fs.CreateDocument(new MemoryStream(Pattern(size, size)), "Doc" + size);
            byte[] file = Write(fs);
            fs.Close();

            NPOIFSFileSystem reread = new NPOIFSFileSystem(new MemoryStream(file));
            ClassicAssert.AreEqual(4096, reread.GetBigBlockSize());
            reread.Close();

            Dictionary<string, byte[]> read = ReadAll(file);
            foreach(int size in BoundarySizes)
                CollectionAssert.AreEqual(Pattern(size, size), read["Doc" + size], "size " + size);
            foreach(var kv in before)
                CollectionAssert.AreEqual(kv.Value, read[kv.Key], kv.Key);
        }

        [TestCase(4095, 4097)]
        [TestCase(4097, 4095)]
        [TestCase(4096, 4095)]
        [TestCase(4095, 4096)]
        [TestCase(100, 10000)]
        [TestCase(10000, 100)]
        [TestCase(10000, 5000)]
        [TestCase(5000, 10000)]
        [TestCase(300, 64)]
        [TestCase(64, 300)]
        [TestCase(5000, 0)]
        public void TestUpdateAcrossBoundaries(int from, int to)
        {
            byte[] file = BuildFile(("A", from), ("Other", 3000), ("OtherBig", 6000));

            NPOIFSFileSystem fs = new NPOIFSFileSystem(new MemoryStream(file));
            fs.CreateOrUpdateDocument(new MemoryStream(Pattern(to, 99)), "A");
            byte[] updated = Write(fs);
            fs.Close();

            Dictionary<string, byte[]> read = ReadAll(updated);
            CollectionAssert.AreEqual(Pattern(to, 99), read["A"]);
            CollectionAssert.AreEqual(Pattern(3000, 7), read["Other"]);
            CollectionAssert.AreEqual(Pattern(6000, 7), read["OtherBig"]);
        }

        // ---- Malformed input ----
        // Raw offsets below assume 512 byte sectors and a single FAT / SBAT / directory sector,
        //  which holds for the small files built by BuildFile

        private static int ReadInt(byte[] b, int off)
        {
            return BitConverter.ToInt32(b, off);
        }

        private static void WriteInt(byte[] b, int off, int v)
        {
            BitConverter.GetBytes(v).CopyTo(b, off);
        }

        private static int DirSectorOffset(byte[] file)
        {
            return (ReadInt(file, 0x30) + 1) * 512;
        }

        private static int DirEntryOffset(byte[] file, string name)
        {
            int sector = DirSectorOffset(file);
            for(int k = 0; k < 4; k++)
            {
                int off = sector + 128 * k;
                int len = BitConverter.ToInt16(file, off + 0x40);
                if(len >= 2 && Encoding.Unicode.GetString(file, off, len - 2) == name)
                    return off;
            }
            throw new ArgumentException("No entry " + name);
        }

        private static int StartOf(byte[] f, string name)
        {
            return ReadInt(f, DirEntryOffset(f, name) + 0x74);
        }

        private static void SetStart(byte[] f, string name, int v)
        {
            WriteInt(f, DirEntryOffset(f, name) + 0x74, v);
        }

        private static void SetSize(byte[] f, string name, int v)
        {
            WriteInt(f, DirEntryOffset(f, name) + 0x78, v);
        }

        private static void SetFat(byte[] f, int sector, int next)
        {
            WriteInt(f, (ReadInt(f, 0x4C) + 1) * 512 + 4 * sector, next);
        }

        private static void SetSbat(byte[] f, int miniSector, int next)
        {
            WriteInt(f, (ReadInt(f, 0x3C) + 1) * 512 + 4 * miniSector, next);
        }

        private static byte[] Truncate(byte[] f, int len)
        {
            byte[] t = new byte[len];
            Array.Copy(f, t, len);
            return t;
        }

        private static IEnumerable<TestCaseData> MalformedCases()
        {
            Type loop = typeof(InvalidOperationException);
            Type badRef = typeof(IndexOutOfRangeException);
            Type meta = typeof(IOException);

            TestCaseData C(string name, Type expected, Action<byte[]> mutate)
            {
                return new TestCaseData(mutate, expected).SetName("Malformed_" + name);
            }
            TestCaseData T(string name, Type expected, Func<byte[], byte[]> mutate)
            {
                return new TestCaseData(mutate, expected).SetName("Malformed_" + name);
            }

            // Big block (FAT) chains
            yield return C("BigSelfLoop", loop, f => SetFat(f, StartOf(f, "Big"), StartOf(f, "Big")));
            yield return C("BigNextBeyondFile", badRef, f => SetFat(f, StartOf(f, "Big"), 100));
            yield return C("BigNextBeyondFat", badRef, f => SetFat(f, StartOf(f, "Big"), 5000));
            yield return C("BigNextHuge", badRef, f => SetFat(f, StartOf(f, "Big"), 0x7FFFFF00));
            yield return C("BigNextOverflowsToBlock0", badRef, f => SetFat(f, StartOf(f, "Big"), 0x800000));
            yield return C("BigNextFatMarker", badRef, f => SetFat(f, StartOf(f, "Big"), -3));
            yield return C("BigNextUnused", badRef, f => SetFat(f, StartOf(f, "Big"), -1));
            yield return C("BigNextNegative", badRef, f => SetFat(f, StartOf(f, "Big"), -1000));
            yield return C("BigStartHuge", badRef, f => SetStart(f, "Big", 0x7FFFFF00));
            yield return C("BigStartNegative", badRef, f => SetStart(f, "Big", -5));
            yield return C("BigSizeLongerThanChain", badRef, f => SetSize(f, "Big", 50000));
            yield return C("BigSizeMax", badRef, f => SetSize(f, "Big", int.MaxValue));
            // Negative sizes keep their pre-existing NPOI.Util.RuntimeException (see TestBugs.Test61300)
            yield return C("SizeNegative", typeof(NPOI.Util.RuntimeException), f => SetSize(f, "Big", -10));

            // Mini block (SBAT) chains
            yield return C("MiniSelfLoop", loop, f => SetSbat(f, StartOf(f, "Mini"), StartOf(f, "Mini")));
            yield return C("MiniSelfLoopRootSizeUnderstated", loop, f =>
            {
                SetSbat(f, StartOf(f, "Mini"), StartOf(f, "Mini"));
                SetSize(f, "Root Entry", 0);
            });
            yield return C("MiniNextBeyondMiniStream", badRef, f => SetSbat(f, StartOf(f, "Mini"), 100));
            yield return C("MiniNextBeyondSbat", badRef, f => SetSbat(f, StartOf(f, "Mini"), 1000));
            yield return C("MiniNextHuge", badRef, f => SetSbat(f, StartOf(f, "Mini"), 0x7FFFFF00));
            yield return C("MiniNextFatMarker", badRef, f => SetSbat(f, StartOf(f, "Mini"), -3));
            yield return C("MiniStartHuge", badRef, f => SetStart(f, "Mini", 0x7FFFFF00));
            yield return C("MiniSizeLongerThanChain", badRef, f => SetSize(f, "Mini", 4095));
            yield return C("MiniStreamStartHuge", badRef, f => SetStart(f, "Root Entry", 0x7FFFFF00));

            // Structures
            yield return C("DirectorySectorLoop", loop, f => SetFat(f, ReadInt(f, 0x30), ReadInt(f, 0x30)));
            yield return C("DirectoryStartHuge", badRef, f => WriteInt(f, 0x30, 0x7FFFFF00));
            yield return C("SiblingSelfCycle", meta, f =>
            {
                int o = DirEntryOffset(f, "Big");
                WriteInt(f, o + 0x44, (o - DirSectorOffset(f)) / 128);
            });
            yield return C("SbatLoop", loop, f =>
            {
                SetFat(f, ReadInt(f, 0x3C), ReadInt(f, 0x3C));
                WriteInt(f, 0x40, 1000);
            });
            yield return C("SbatStartHuge", badRef, f => WriteInt(f, 0x3C, 0x7FFFFF00));
            yield return C("FatSectorHuge", badRef, f => WriteInt(f, 0x4C, 0x7FFFFF00));
            yield return C("FatCountHuge", meta, f => WriteInt(f, 0x2C, 1000000));
            yield return C("XbatLoop", loop, f =>
            {
                WriteInt(f, 0x48, 1000000);
                WriteInt(f, 0x44, 0);
            });
            yield return T("TruncatedHalf", badRef, f => Truncate(f, f.Length / 2));
            yield return T("TruncatedAfterHeader", badRef, f => Truncate(f, 600));
        }

        private static byte[] MalformedBase()
        {
            return BuildFile(("Big", 5000), ("Mini", 300));
        }

        [TestCaseSource(nameof(MalformedCases))]
        public void TestMalformed(Delegate mutate, Type expected)
        {
            byte[] file = MalformedBase();
            if(mutate is Func<byte[], byte[]> f)
                file = f(file);
            else
                ((Action<byte[]>) mutate)(file);

            Exception thrown = RunWithTimeout(() =>
            {
                try
                {
                    ReadAll(file);
                    return null;
                }
                catch(Exception e)
                {
                    return e;
                }
            });

            ClassicAssert.IsNotNull(thrown, "Malformed input was read without error");
            ClassicAssert.AreEqual(expected, thrown.GetType(), thrown.ToString());
        }

        [Test]
        public void TestPropertyIndexOutOfRangeIsSkipped()
        {
            byte[] file = MalformedBase();
            // Point the Big entry's left sibling at a property index that does not exist
            WriteInt(file, DirEntryOffset(file, "Big") + 0x44, 1000);

            Dictionary<string, byte[]> read = RunWithTimeout(() => ReadAll(file));
            CollectionAssert.AreEqual(Pattern(5000, 7), read["Big"]);
        }

        [Test]
        public void TestRootChildIndexOutOfRangeIsSkipped()
        {
            byte[] file = MalformedBase();
            WriteInt(file, DirEntryOffset(file, "Root Entry") + 0x4C, 1000);

            Dictionary<string, byte[]> read = RunWithTimeout(() => ReadAll(file));
            ClassicAssert.AreEqual(0, read.Count);
        }

        [Test]
        public void TestEmptyRootMiniStreamStartingAtUnusedBlock()
        {
            byte[] file = BuildFile(("Big", 5000));
            SetStart(file, "Root Entry", -1);

            Dictionary<string, byte[]> read = RunWithTimeout(() => ReadAll(file));
            CollectionAssert.AreEqual(Pattern(5000, 7), read["Big"]);
        }

        [Test]
        public void TestMiniStreamRandomAccessOnLargeMiniStream()
        {
            // 400 docs of 64 bytes each spans many big blocks of mini stream
            var docs = new List<(string, int)>();
            for(int i = 0; i < 400; i++)
                docs.Add(("D" + i, 64));
            byte[] file = BuildFile(docs.ToArray());

            Dictionary<string, byte[]> read = RunWithTimeout(() => ReadAll(file));
            ClassicAssert.AreEqual(400, read.Count);
            CollectionAssert.AreEqual(Pattern(64, 7), read["D399"]);
        }

        [TestCase("BigSelfLoop")]
        [TestCase("BigNextBeyondFile")]
        [TestCase("BigSizeLongerThanChain")]
        [TestCase("MiniSelfLoop")]
        [TestCase("MiniNextBeyondMiniStream")]
        public void TestMalformedLegacyOPOIFS(string kind)
        {
            byte[] f = MalformedBase();
            switch(kind)
            {
                case "BigSelfLoop":
                    SetFat(f, StartOf(f, "Big"), StartOf(f, "Big"));
                    break;
                case "BigNextBeyondFile":
                    SetFat(f, StartOf(f, "Big"), 100);
                    break;
                case "BigSizeLongerThanChain":
                    SetSize(f, "Big", 50000);
                    break;
                case "MiniSelfLoop":
                    SetSbat(f, StartOf(f, "Mini"), StartOf(f, "Mini"));
                    break;
                case "MiniNextBeyondMiniStream":
                    SetSbat(f, StartOf(f, "Mini"), 100);
                    break;
            }

            Exception thrown = RunWithTimeout(() =>
            {
                try
                {
                    OPOIFSFileSystem fs = new OPOIFSFileSystem(new MemoryStream(f));
                    Walk(fs.Root, "", new Dictionary<string, byte[]>());
                    return null;
                }
                catch(Exception e)
                {
                    return e;
                }
            });

            // The legacy reader reports malformed chains with IOException, IndexOutOfRangeException
            // or InvalidOperationException; it must fail cleanly and never hang.
            ClassicAssert.IsNotNull(thrown, "Malformed input was read without error");
            ClassicAssert.IsTrue(thrown is IOException || thrown is IndexOutOfRangeException
                || thrown is InvalidOperationException, thrown.ToString());
        }
    }
}