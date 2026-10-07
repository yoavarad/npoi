using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NPOI.OpenXml4Net.Util
{
    /**
     * A ZipEntrySource wrapper around a ZipFile.
     * Should be as low in terms of memory as a
     *  normal ZipFile implementation is.
     */
    public class ZipFileZipEntrySource : ZipEntrySource
    {
        private ZipFile zipArchive;
        public ZipFileZipEntrySource(ZipFile zipFile)
        {
            try
            {
                ZipSecureFile.CheckEntryCount(zipFile.Count);
            }
            catch
            {
                zipFile.Close();
                throw;
            }
            this.zipArchive = zipFile;
        }

        public void Close()
        {
            if(zipArchive != null)
            {
                zipArchive.Close();
            }
            zipArchive = null;
        }

        public bool IsClosed
        {
            get { return zipArchive == null; }
        }

        public IEnumerator Entries
        {
            get
            {
                if(zipArchive == null)
                    throw new InvalidDataException("Zip File is closed");
                return zipArchive.GetEnumerator();

            }
        }

        public Stream GetInputStream(ZipEntry entry)
        {
            if(zipArchive == null)
                throw new InvalidDataException("Zip File is closed");
            return ZipSecureFile.AddThreshold(zipArchive.GetInputStream(entry), entry);
        }
    }
}