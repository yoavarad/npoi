using NPOI.OpenXmlFormats.Dml.Chart;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace NPOI.OpenXmlFormats.Dml
{
    public class ChartSpaceDocument
    {
        CT_ChartSpace chartSpace = null;

        public ChartSpaceDocument()
        {
            chartSpace = new CT_ChartSpace();
        }
        public ChartSpaceDocument(CT_ChartSpace chartspace)
        {
            this.chartSpace = chartspace;
        }
        public static ChartSpaceDocument Parse(XmlDocument xmldoc, XmlNamespaceManager namespaceMgr)
        {
            CT_ChartSpace obj = CT_ChartSpace.Parse(xmldoc.DocumentElement, namespaceMgr);
            return new ChartSpaceDocument(obj);
        }
        public CT_ChartSpace GetChartSpace()
        {
            return chartSpace;
        }
        public void SetChartSpace(CT_ChartSpace chartspace)
        {
            this.chartSpace = chartspace;
        }
        public void Save(Stream stream)
        {
            chartSpace.Write(stream);
        }
    }
}