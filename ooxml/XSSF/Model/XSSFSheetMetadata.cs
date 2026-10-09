using NPOI.OpenXml4Net.OPC;
using System.IO;
using System.Text;

namespace NPOI.XSSF.Model
{
    /// <summary>
    /// The workbook's cell metadata part (<c>/xl/metadata.xml</c>). Excel marks a dynamic-array
    /// formula by pointing its anchor cell's <c>cm</c> attribute at an <c>XLDAPR</c> cell-metadata
    /// record in this part. A part read from a file is kept byte-for-byte; a new part holds just the
    /// single dynamic-array record that <c>cm="1"</c> refers to.
    /// </summary>
    public class XSSFSheetMetadata : POIXMLDocumentPart
    {
        private const string DynamicArrayMetadata =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n"
            + "<metadata xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:xda=\"http://schemas.microsoft.com/office/spreadsheetml/2017/dynamicarray\">"
            + "<metadataTypes count=\"1\"><metadataType name=\"XLDAPR\" minSupportedVersion=\"120000\" copy=\"1\" pasteAll=\"1\" pasteValues=\"1\" merge=\"1\" splitFirst=\"1\" rowColShift=\"1\" clearFormats=\"1\" clearComments=\"1\" assign=\"1\" coerce=\"1\" cellMeta=\"1\"/></metadataTypes>"
            + "<futureMetadata name=\"XLDAPR\" count=\"1\"><bk><extLst><ext uri=\"{bdbb8cdc-fa1e-496e-a857-3c3f30c029c3}\"><xda:dynamicArrayProperties fDynamic=\"1\" fCollapsed=\"0\"/></ext></extLst></bk></futureMetadata>"
            + "<cellMetadata count=\"1\"><bk><rc t=\"1\" v=\"0\"/></bk></cellMetadata>"
            + "</metadata>";

        private readonly bool _isNew;

        public XSSFSheetMetadata()
            : base()
        {
            _isNew = true;
        }

        internal XSSFSheetMetadata(PackagePart part)
            : base(part)
        {
        }

        protected internal override void Commit()
        {
            if(!_isNew)
            {
                return;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(DynamicArrayMetadata);
            using(Stream out1 = GetPackagePart().GetOutputStream())
            {
                out1.Write(bytes, 0, bytes.Length);
            }
        }
    }
}