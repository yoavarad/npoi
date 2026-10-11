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
    /// A POIFS DataSource backed by a File.
    /// <para>
    /// The Java original memory-maps the file, so changes to the returned buffers
    /// end up on disk. This port copies the file into memory (no file handle is
    /// held open) and hands out buffers that are views over that in-memory image,
    /// so block changes are kept. In writable mode, <see cref="Flush"/> writes the
    /// image back to the file; <c>NPOIFSFileSystem.WriteFileSystem()</c> calls it.
    /// </para>
    /// </summary>
    public class FileBackedDataSource : DataSource
    {
        private readonly ByteArrayBackedDataSource data;
        private readonly string path;
        private readonly bool writable;

        public FileBackedDataSource(FileInfo file)
            : this(file, false)
        {

        }
        public FileBackedDataSource(FileInfo file, bool readOnly)
        {
            if(!file.Exists)
                throw new FileNotFoundException(file.FullName);
            // The data is copied into memory, so release the file handle straight away
            using(FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                byte[] temp = new byte[stream.Length];
                IOUtils.ReadFully(stream, temp);
                data = new ByteArrayBackedDataSource(temp);
            }
            this.path = file.FullName;
            this.writable = !readOnly;
        }
        public FileBackedDataSource(FileStream stream, bool readOnly)
        {
            stream.Position = 0;
            byte[] temp = new byte[stream.Length];
            IOUtils.ReadFully(stream, temp);
            data = new ByteArrayBackedDataSource(temp);
            this.path = stream.Name;
            this.writable = !readOnly;
            stream.Position = 0;
        }

        /// <summary>
        /// Same as <see cref="Close"/>.
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        public bool IsWriteable
        {
            get
            {
                return this.writable;
            }
        }

        /// <summary>
        /// A new read-only stream over the current in-memory contents
        /// (a snapshot: it does not follow later growth of the data).
        /// </summary>
        public Stream Stream
        {
            get { return new MemoryStream(data.GetBackingArray(), 0, (int) data.Size, false); }
        }

        /// <summary>
        /// Reads a sequence of bytes starting at the given file position. The returned
        /// buffer is a view over the in-memory image, starting at position 0, so changes to it are kept and
        /// reach the file on <see cref="Flush"/>.
        /// </summary>
        /// <param name="length"></param>
        /// <param name="position">The file position at which the transfer is to begin;</param>
        /// <returns></returns>
        public override ByteBuffer Read(int length, long position)
        {
            if(position >= Size)
                throw new IndexOutOfRangeException("Position " + position + " past the end of the file");
            // Slice so the buffer starts at position 0 (as Java's mapped buffer does)
            //  while still sharing the in-memory image
            return data.Read(length, position).Slice();
        }
        /// <summary>
        /// Writes a sequence of bytes from the given buffer into the in-memory image,
        /// starting at the given file position, extending it if needed.
        /// </summary>
        /// <param name="src">The buffer from which bytes are to be transferred</param>
        /// <param name="position">The file position at which the transfer is to begin;
        /// must be non-negative</param>
        public override void Write(ByteBuffer src, long position)
        {
            data.Write(src, position);
        }

        public override void CopyTo(Stream stream)
        {
            data.CopyTo(stream);
        }

        /// <summary>
        /// Writes the in-memory image back to the file it was read from,
        /// replacing the file's contents.
        /// </summary>
        /// <exception cref="InvalidOperationException">if opened read-only or already closed</exception>
        public void Flush()
        {
            if(!writable)
                throw new InvalidOperationException("Opened read-only, cannot write back to " + path);
            if(data.Size < 0)
                throw new InvalidOperationException("Already closed, cannot write back to " + path);

            // Write to a temp file next to the target and swap it in, so a failed
            //  write does not leave the original truncated
            string tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using(FileStream stream = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    data.CopyTo(stream);
                }
                if(File.Exists(path))
                    File.Replace(tmp, path, null);
                else
                    File.Move(tmp, path);
            }
            finally
            {
                if(File.Exists(tmp))
                    File.Delete(tmp);
            }
        }

        public override long Size
        {
            get { return data.Size; }
        }

        public override void Close()
        {
            data.Close();
        }
    }

}