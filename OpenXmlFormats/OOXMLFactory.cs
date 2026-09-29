using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace NPOI.OpenXmlFormats
{
    public class OOXMLFactory<T>
    {
        XmlSerializer serializerObj = null;
        public OOXMLFactory()
        {
            serializerObj = new XmlSerializer(typeof(T));
        }

        public T Parse(Stream stream)
        {
            T obj = (T)serializerObj.Deserialize(stream);
            stream.Close();
            return obj;
        }
        public T Create()
        {
            return (T) Activator.CreateInstance(typeof(T));
        }
    }
}