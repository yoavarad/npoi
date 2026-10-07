/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
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
using NPOI.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.IO;

namespace TestCases.POIFS.FileSystem
{
    /// <summary>
    /// Streaming write, document store and skip behaviour of the NPOIFS classes (task #106)
    /// </summary>
    [TestFixture]
    public class TestNPOIFSStreamingWrite
    {
        private static byte[] Pattern(int length)
        {
            byte[] data = new byte[length];
            for(int i = 0; i < length; i++)
            {
                data[i] = (byte) (i * 7 + 3);
            }
            return data;
        }

        private static byte[] ReadAll(DocumentEntry entry)
        {
            using NDocumentInputStream dis = new NDocumentInputStream(entry);
            byte[] data = new byte[entry.Size];
            dis.ReadFully(data);
            return data;
        }

        private static NPOIFSFileSystem RoundTrip(NPOIFSFileSystem fs)
        {
            MemoryStream ms = new MemoryStream();
            fs.WriteFileSystem(ms);
            fs.Close();
            return new NPOIFSFileSystem(new MemoryStream(ms.ToArray()));
        }

        [Test]
        public void OutputStreamWriteByteIsStoredInBlocks()
        {
            NPOIFSFileSystem fs = new NPOIFSFileSystem();
            NPOIFSStream stream = new NPOIFSStream(fs);
            byte[] data = Pattern(700);

            Stream os = stream.GetOutputStream();
            foreach(byte b in data)
            {
                os.WriteByte(b);
            }
            os.Close();

            MemoryStream read = new MemoryStream();
            foreach(ByteBuffer block in stream)
            {
                for(int i = block.Position; i < block.Position + block.Remain; i++)
                {
                    read.WriteByte(block[i]);
                }
            }
            byte[] stored = read.ToArray();
            ClassicAssert.AreEqual(1024, stored.Length, "two 512 byte blocks");
            ClassicAssert.AreEqual(data, Arrays.CopyOf(stored, data.Length));
            fs.Close();
        }

        // Readers pick the mini or big store from the size alone, so the
        //  round-tripped contents only match if Store chose the same one
        [TestCase(0, false)]
        [TestCase(100, false)]
        [TestCase(4095, false)]
        [TestCase(4096, false)]
        [TestCase(10000, false)]
        [TestCase(0, true)]
        [TestCase(4095, true)]
        [TestCase(4096, true)]
        [TestCase(10000, true)]
        public void CreateDocumentRoundTrips(int length, bool seekable)
        {
            byte[] data = Pattern(length);
            NPOIFSFileSystem fs = new NPOIFSFileSystem();
            fs.Root.CreateDocument("Doc", seekable ? new MemoryStream(data) : new NonSeekableStream(data));

            fs = RoundTrip(fs);
            DocumentEntry entry = (DocumentEntry) fs.Root.GetEntry("Doc");
            ClassicAssert.AreEqual(length, entry.Size);
            ClassicAssert.AreEqual(data, ReadAll(entry));
            fs.Close();
        }

        [Test]
        public void CreateDocumentStoresFromCurrentPosition()
        {
            // 3000 bytes left after the position: must go to the mini stream,
            //  even though the stream's Length is above the cut-off
            byte[] data = Pattern(6000);
            MemoryStream source = new MemoryStream(data);
            source.Position = 3000;
            NPOIFSFileSystem fs = new NPOIFSFileSystem();
            fs.Root.CreateDocument("Doc", source);

            fs = RoundTrip(fs);
            DocumentEntry entry = (DocumentEntry) fs.Root.GetEntry("Doc");
            ClassicAssert.AreEqual(3000, entry.Size);
            ClassicAssert.AreEqual(Arrays.CopyOfRange(data, 3000, 6000), ReadAll(entry));
            fs.Close();
        }

        [Test]
        public void SkipAcrossBlocksAndPastEnd()
        {
            byte[] data = Pattern(20000);
            NPOIFSFileSystem fs = new NPOIFSFileSystem();
            fs.Root.CreateDocument("Doc", new MemoryStream(data));
            DocumentEntry entry = (DocumentEntry) fs.Root.GetEntry("Doc");

            using(NDocumentInputStream dis = new NDocumentInputStream(entry))
            {
                ClassicAssert.AreEqual(9001, dis.Skip(9001));
                ClassicAssert.AreEqual(data[9001], (byte) dis.ReadByte());
                ClassicAssert.AreEqual(20000 - 9002, dis.Skip(1000000000L));
                ClassicAssert.AreEqual(-1, dis.ReadByte());
            }
            fs.Close();
        }

        [Test]
        public void CreateTruncatesExistingFile()
        {
            FileInfo file = TempFile.CreateTempFile("TestNPOIFSStreamingWrite", ".ole2");
            File.WriteAllBytes(file.FullName, Pattern(100000));

            POIFSFileSystem.Create(file).Close();

            MemoryStream empty = new MemoryStream();
            using(NPOIFSFileSystem expected = new NPOIFSFileSystem())
            {
                expected.WriteFileSystem(empty);
            }
            file.Refresh();
            ClassicAssert.AreEqual(empty.Length, file.Length);
            using(NPOIFSFileSystem fs = new NPOIFSFileSystem(file, true))
            {
                ClassicAssert.AreEqual(0, fs.Root.EntryCount);
            }
        }

        private sealed class NonSeekableStream : Stream
        {
            private readonly MemoryStream inner;

            public NonSeekableStream(byte[] data)
            {
                inner = new MemoryStream(data);
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }
            public override void Flush()
            {
            }
            public override int Read(byte[] buffer, int offset, int count)
            {
                return inner.Read(buffer, offset, Math.Min(count, 300));
            }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}