using ICSharpCode.SharpZipLib.Zip;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NPOI.OpenXml4Net.OPC.Internal.Marshallers
{
    /**
     * Package core properties marshaller specialized for zipped package.
     *
     * @author Julien Chable
     */
    public class ZipPackagePropertiesMarshaller : PackagePropertiesMarshaller
    {
        private static readonly POILogger logger = POILogFactory.GetLogger(typeof(ZipPackagePropertiesMarshaller));

        public override bool Marshall(PackagePart part, Stream out1)
        {
            if(out1 is not ZipOutputStream zos)
            {
                throw new ArgumentException("ZipOutputStream expected!");
            }

            // Saving the part in the zip file
            string name = ZipHelper
                .GetZipItemNameFromOPCName(part.PartName.URI.ToString());
            ZipEntry ctEntry = new ZipEntry(name) { DateTime = ZipHelper.ZipEntryTimestamp };

            try
            {
                // Save in ZIP
                zos.PutNextEntry(ctEntry); // Add entry in ZIP

                base.Marshall(part, zos); // Marshall the properties inside a XML
                                          // Document
                StreamHelper.SaveXmlInStream(xmlDoc, zos);

                zos.CloseEntry();
            }
            catch(IOException e)
            {
                throw new OpenXml4NetException(e.Message, e);
            }
            catch(Exception e)
            {
                logger.Log(POILogger.WARN, "Can't marshall package properties part", e);
                return false;
            }
            return true;
        }
    }

}