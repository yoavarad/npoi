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

using NPOI.Util;
using System;
using System.IO;

namespace NPOI.POIFS.NIO
{
    /// <summary>
    /// A read-only POIFS DataSource that reads blocks on demand from a seekable stream
    /// owned by the caller. Nothing is buffered up front, and the stream is never closed
    /// or disposed. Each read seeks to the block and puts the stream position back after,
    /// so the caller's position is unchanged. The caller must keep the stream open and
    /// unchanged while the file system is in use.
    /// </summary>
    public class StreamBackedDataSource : DataSource
    {
        private readonly Stream stream;
        private long size;
        private bool closed;

        public StreamBackedDataSource(Stream stream)
        {
            if(stream == null)
                throw new ArgumentNullException(nameof(stream));
            if(!stream.CanRead || !stream.CanSeek)
                throw new ArgumentException("The stream must be readable and seekable", nameof(stream));
            this.stream = stream;
            this.size = stream.Length;
        }

        public override ByteBuffer Read(int length, long position)
        {
            if(closed)
                throw new InvalidOperationException("The data source is closed");
            if(position < 0 || position >= size)
            {
                throw new IndexOutOfRangeException(
                      "Unable to read " + length + " bytes from " +
                      position + " in stream of length " + size
                );
            }

            int toRead = (int) Math.Min(length, size - position);
            byte[] buffer = new byte[toRead];
            long saved = stream.Position;
            try
            {
                stream.Position = position;
                int read = toRead == 0 ? 0 : IOUtils.ReadFully(stream, buffer);
                if(read != toRead)
                    throw new IOException("Unable to read " + toRead + " bytes from " + position + ": the stream ended after " + Math.Max(read, 0));
            }
            finally
            {
                stream.Position = saved;
            }
            return ByteBuffer.CreateBuffer(buffer);
        }

        /// <exception cref="NotSupportedException">always: this data source is read-only</exception>
        public override void Write(ByteBuffer src, long position)
        {
            throw new NotSupportedException("StreamBackedDataSource is read-only");
        }

        public override void CopyTo(Stream output)
        {
            if(closed)
                throw new InvalidOperationException("The data source is closed");
            long saved = stream.Position;
            try
            {
                stream.Position = 0;
                byte[] buffer = new byte[81920];
                long remaining = size;
                while(remaining > 0)
                {
                    int read = stream.Read(buffer, 0, (int) Math.Min(buffer.Length, remaining));
                    if(read <= 0)
                        throw new IOException("The stream ended " + remaining + " bytes early");
                    output.Write(buffer, 0, read);
                    remaining -= read;
                }
            }
            finally
            {
                stream.Position = saved;
            }
        }

        /// <summary>
        /// Caps the visible size, as the byte[] path does with the maximum size the header's FAT can address.
        /// </summary>
        internal void LimitSize(long maxSize)
        {
            if(maxSize < size)
                size = maxSize;
        }

        public override long Size
        {
            get { return size; }
        }

        /// <summary>
        /// Stops further reads. The caller's stream is left open.
        /// </summary>
        public override void Close()
        {
            closed = true;
        }
    }
}