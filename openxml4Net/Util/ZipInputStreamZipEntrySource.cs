using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NPOI.OpenXml4Net.Util
{
    /**
     * Provides a way to get at all the ZipEntries
     *  from a ZipInputStream, as many times as required.
     * Allows a ZipInputStream to be treated much like
     *  a ZipFile, for a price in terms of memory.
     * Be sure to call {@link #close()} as soon as you're
     *  done, to free up that memory!
     */
    public class ZipInputStreamZipEntrySource : ZipEntrySource
    {
        private List<FakeZipEntry> zipEntries;

        /**
         * Reads all the entries from the ZipInputStream 
         *  into memory, and closes the source stream.
         * We'll then eat lots of memory, but be able to
         *  work with the entries at-will.
         */
        public ZipInputStreamZipEntrySource(ZipInputStream inp)
        {
            zipEntries = new List<FakeZipEntry>();

            bool going = true;
            long entryCount = 0;
            //if(inp.Position != 0)
            //    inp.Position = 0;
            while(going)
            {
                ZipEntry zipEntry = inp.GetNextEntry();
                if(zipEntry == null)
                {
                    going = false;
                }
                else
                {
                    ZipSecureFile.CheckEntryCount(++entryCount);
                    FakeZipEntry entry = new FakeZipEntry(zipEntry, inp);
                    //inp.Close();

                    zipEntries.Add(entry);
                }
            }
            inp.Close();
        }

        public IEnumerator Entries
        {
            get
            {
                return new EntryEnumerator(zipEntries);
            }
        }

        public Stream GetInputStream(ZipEntry zipEntry)
        {
            FakeZipEntry entry = (FakeZipEntry)zipEntry;
            return entry.GetInputStream();
        }

        public void Close()
        {
            // Free the memory
            zipEntries = null;
        }

        public bool IsClosed
        {
            get { return zipEntries == null; }
        }
        /**
         * Why oh why oh why are Iterator and Enumeration
         *  still not compatible?
         */
        internal sealed class EntryEnumerator : IEnumerator
        {
            private List<FakeZipEntry>.Enumerator iterator;

            internal EntryEnumerator(List<FakeZipEntry> zipEntries)
            {
                iterator = zipEntries.GetEnumerator();
            }

            public bool MoveNext()
            {
                return iterator.MoveNext();
            }

            public object Current
            {
                get
                {
                    return iterator.Current;
                }
            }

            #region IEnumerator Members


            public void Reset()
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        /**
         * So we can close the real zip entry and still
         *  effectively work with it.
         * Holds the (decompressed!) data in memory, so
         *  close this as soon as you can! 
         */
        public class FakeZipEntry : ZipEntry
        {
            private byte[] data;

            public FakeZipEntry(ZipEntry entry, ZipInputStream inp) : base(entry.Name)
            {

                // Grab the de-compressed contents for later.
                // Header sizes are untrusted: never pre-allocate from them beyond a small cap,
                // and enforce limits on the bytes actually inflated.
                long entrySize = entry.Size;
                if(entrySize >= Int32.MaxValue)
                {
                    throw new IOException("ZIP entry size is too large");
                }
                MemoryStream baos = new MemoryStream(entrySize > 0 ? (int) Math.Min(entrySize, 1 << 20) : 0);

                Func<long> compressed = ZipSecureFile.CompressedCounter(inp, entry.CompressionMethod == CompressionMethod.Deflated);
                byte[] buffer = new byte[4096];
                long total = 0;
                int read;
                while((read = inp.Read(buffer, 0, buffer.Length)) > 0)
                {
                    total += read;
                    ZipSecureFile.CheckThreshold(total, compressed);
                    baos.Write(buffer, 0, read);
                }

                data = baos.ToArray();
            }

            public Stream GetInputStream()
            {
                return new MemoryStream(data);
            }
        }
    }

}