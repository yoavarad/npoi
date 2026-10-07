using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NPOI.OpenXml4Net.Util
{
    public class ZipSecureFile : ZipFile
    {
        private static double MIN_INFLATE_RATIO = 0.01d;
        private static long MAX_ENTRY_SIZE = 0xFFFFFFFFL;

        // don't alert for expanded sizes smaller than 100k
        private static long GRACE_ENTRY_SIZE = 100*1024L;

        // The default maximum size of extracted text 
        private static long MAX_TEXT_SIZE = 10*1024*1024L;

        /**
         * Sets the ratio between de- and inflated bytes to detect zipbomb.
         * It defaults to 1% (= 0.01d), i.e. when the compression is better than
         * 1% for any given read package part, the parsing will fail indicating a 
         * Zip-Bomb.
         *
         * @param ratio the ratio between de- and inflated bytes to detect zipbomb
         */
        public static void SetMinInflateRatio(double ratio)
        {
            if(double.IsNaN(ratio) || ratio < 0 || ratio > 1)
            {
                throw new ArgumentException("Min inflate ratio is bounded [0-1], but had " + ratio);
            }
            MIN_INFLATE_RATIO = ratio;
        }

        /**
         * Returns the current minimum compression rate that is used.
         * 
         * See setMinInflateRatio() for details.
         *
         * @return The min accepted compression-ratio.  
         */
        public static double GetMinInflateRatio()
        {
            return MIN_INFLATE_RATIO;
        }

        /**
         * Sets the maximum file size of a single zip entry. It defaults to 4GB,
         * i.e. the 32-bit zip format maximum.
         * 
         * This can be used to limit memory consumption and protect against 
         * security vulnerabilities when documents are provided by users.
         *
         * @param maxEntrySize the max. file size of a single zip entry
         */
        public static void SetMaxEntrySize(long maxEntrySize)
        {
            if(maxEntrySize < 0 || maxEntrySize > 0xFFFFFFFFL)
            {
                throw new ArgumentException("Max entry size is bounded [0-4GB].");
            }
            MAX_ENTRY_SIZE = maxEntrySize;
        }

        /**
         * Returns the current maximum allowed uncompressed file size.
         * 
         * See setMaxEntrySize() for details.
         *
         * @return The max accepted uncompressed file size. 
         */
        public static long GetMaxEntrySize()
        {
            return MAX_ENTRY_SIZE;
        }


        /// <summary>
        /// <para>
        /// Sets the maximum number of characters of text that are
        /// extracted before an exception is thrown during extracting
        /// text from documents.
        /// </para>
        /// <para>
        /// This can be used to limit memory consumption and protect against
        /// security vulnerabilities when documents are provided by users.
        /// </para>
        /// </summary>
        /// <param name="maxTextSize">the max. file size of a single zip entry</param>
        public static void SetMaxTextSize(long maxTextSize)
        {
            if(maxTextSize < 0 || maxTextSize > 0xFFFFFFFFL)
            {     // don't use MAX_ENTRY_SIZE here!
                throw new ArgumentException("Max text size is bounded [0-4GB], but had " + maxTextSize);
            }
            MAX_TEXT_SIZE = maxTextSize;
        }

        /// <summary>
        /// <para>
        /// Returns the current maximum allowed text size.
        /// </para>
        /// <para>
        /// See SetMaxTextSize() for details.
        /// </para>
        /// </summary>
        /// <returns>The max accepted text size.</returns>
        public static long GetMaxTextSize()
        {
            return MAX_TEXT_SIZE;
        }
        private static long MAX_ENTRY_COUNT = 10000;
        private static long MAX_TOTAL_SIZE = 0xFFFFFFFFL;

        /// <summary>
        /// Sets the maximum total uncompressed size of all entries buffered in memory when a
        /// package is read from a stream. Defaults to 4GB.
        /// </summary>
        public static void SetMaxTotalSize(long maxTotalSize)
        {
            if(maxTotalSize < 0)
            {
                throw new ArgumentException("Max total size must not be negative.");
            }
            MAX_TOTAL_SIZE = maxTotalSize;
        }

        /// <summary>Returns the maximum total uncompressed size allowed when buffering a zip stream.</summary>
        public static long GetMaxTotalSize()
        {
            return MAX_TOTAL_SIZE;
        }

        internal static void CheckTotalSize(long total)
        {
            if(total > MAX_TOTAL_SIZE)
            {
                throw new ZipSecurityException("Zip bomb detected! The zip would exceed the max total size of expanded data. "
                    + "You can adjust this limit via ZipSecureFile.SetMaxTotalSize(). "
                    + "Total: " + total + ", limit: MAX_TOTAL_SIZE: " + MAX_TOTAL_SIZE);
            }
        }

        /// <summary>
        /// Sets the maximum number of entries a zip may contain. Defaults to 10000.
        /// </summary>
        public static void SetMaxEntryCount(long maxEntryCount)
        {
            if(maxEntryCount < 0)
            {
                throw new ArgumentException("Max entry count must not be negative.");
            }
            MAX_ENTRY_COUNT = maxEntryCount;
        }

        /// <summary>Returns the maximum number of zip entries allowed.</summary>
        public static long GetMaxEntryCount()
        {
            return MAX_ENTRY_COUNT;
        }

        public ZipSecureFile(FileStream file, int mode)
            : base(file)
        {
            CheckEntryCountOrClose();
        }

        public ZipSecureFile(FileStream file)
            : base(file)
        {
            CheckEntryCountOrClose();
        }

        public ZipSecureFile(String name)
                : base(name)
        {
            CheckEntryCountOrClose();
        }

        /// <summary>
        /// Opens an entry, enforcing the zip-bomb limits on the bytes actually read.
        /// </summary>
        public new Stream GetInputStream(ZipEntry entry)
        {
            return AddThreshold(base.GetInputStream(entry), entry);
        }

        private void CheckEntryCountOrClose()
        {
            try
            {
                CheckEntryCount(Count);
            }
            catch
            {
                Close();
                throw;
            }
        }

        internal static void CheckEntryCount(long count)
        {
            if(count > MAX_ENTRY_COUNT)
            {
                throw new ZipSecurityException("Zip bomb detected! The zip contains more entries than allowed. "
                    + "You can adjust this limit via ZipSecureFile.SetMaxEntryCount(). "
                    + "Entries: " + count + ", limit: MAX_ENTRY_COUNT: " + MAX_ENTRY_COUNT);
            }
        }

        private static readonly FieldInfo InflaterField =
            typeof(InflaterInputStream).GetField("inf", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Total compressed bytes the inflater of a deflated entry stream has consumed so far.
        /// Taken from the decompressor itself, never from zip headers (which an attacker controls).
        /// Returns null if the stream is not deflated (stored entries cannot expand).
        /// </summary>
        internal static Func<long> CompressedCounter(Stream zipStream, bool deflated)
        {
            if(!deflated)
            {
                // stored entries cannot expand; ZipInputStream is an InflaterInputStream
                // whose inflater stays unused (TotalIn == 0) for them
                return null;
            }
            if(zipStream is InflaterInputStream)
            {
                Inflater inf = InflaterField?.GetValue(zipStream) as Inflater;
                if(inf != null)
                {
                    return () => inf.TotalIn;
                }
            }
            // fail closed: cannot measure the real compressed size
            throw new ZipSecurityException("Zip bomb detection could not be applied to this entry; refusing to read it.");
        }

        /// <summary>
        /// Throws if the counted number of inflated bytes violates a limit.
        /// </summary>
        // A single in-memory buffer (MemoryStream/byte[]) tops out just under 2GB, so paths that buffer
        // whole entries clamp the effective entry limit to this (public defaults unchanged).
        internal static long StreamEntryLimit = 0x7FFFFFC7L;

        internal static void CheckThreshold(long inflated, Func<long> compressed, long entryLimit = long.MaxValue)
        {
            if(inflated > MAX_ENTRY_SIZE || inflated > entryLimit)
            {
                throw new ZipSecurityException("Zip bomb detected! The file would exceed the max size of the expanded data in the zip-file. "
                        + "This may indicate that the file is used to inflate memory usage and thus could pose a security risk. "
                        + "You can adjust this limit via ZipSecureFile.SetMaxEntrySize() if you need to work with files which are very large. "
                        + "Counter: " + inflated + ", Limits: MAX_ENTRY_SIZE: " + MAX_ENTRY_SIZE + ", buffer limit: " + entryLimit);
            }
            if(compressed == null || inflated <= GRACE_ENTRY_SIZE)
            {
                return;
            }
            long comp = compressed();
            double ratio = (double) comp / (double) inflated;
            if(ratio >= MIN_INFLATE_RATIO)
            {
                return;
            }
            throw new ZipSecurityException("Zip bomb detected! The file would exceed the max. ratio of compressed file size to the size of the expanded data. "
                    + "This may indicate that the file is used to inflate memory usage and thus could pose a security risk. "
                    + "You can adjust this limit via ZipSecureFile.SetMinInflateRatio() if you need to work with files which exceed this limit. "
                    + "Counter: " + inflated + ", compressed: " + comp + ", ratio: " + ratio
                    + ", Limits: MIN_INFLATE_RATIO: " + MIN_INFLATE_RATIO);
        }

        /// <summary>
        /// Wraps an entry stream so every read is counted and checked against the limits.
        /// </summary>
        public static Stream AddThreshold(Stream zipStream, ZipEntry entry)
        {
            bool deflated = entry == null || entry.CompressionMethod == CompressionMethod.Deflated;
            try
            {
                return new ThresholdInputStream(zipStream, CompressedCounter(zipStream, deflated));
            }
            catch
            {
                zipStream.Dispose();
                throw;
            }
        }

        private sealed class ThresholdInputStream : Stream
        {
            private readonly Stream input;
            private readonly Func<long> compressed;
            private long counter;

            public ThresholdInputStream(Stream input, Func<long> compressed)
            {
                this.input = input;
                this.compressed = compressed;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int n = input.Read(buffer, offset, count);
                if(n > 0)
                {
                    counter += n;
                    CheckThreshold(counter, compressed);
                }
                return n;
            }

            public override int ReadByte()
            {
                int b = input.ReadByte();
                if(b >= 0)
                {
                    counter++;
                    CheckThreshold(counter, compressed);
                }
                return b;
            }

            public override bool CanRead => input.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if(disposing)
                {
                    input.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>Thrown when a zip violates a ZipSecureFile limit (zip-bomb protection).</summary>
    public class ZipSecurityException : IOException
    {
        public ZipSecurityException(string message) : base(message) { }
    }
}