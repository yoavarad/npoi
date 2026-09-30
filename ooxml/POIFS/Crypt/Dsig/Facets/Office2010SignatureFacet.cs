/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
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

/* ====================================================================
   This product Contains an ASLv2 licensed version of the OOXML signer
   package from the eID Applet project
   http://code.google.com/p/eid-applet/source/browse/tRunk/README.txt  
   Copyright (C) 2008-2014 FedICT.
   ================================================================= */

namespace NPOI.POIFS.Crypt.Dsig.Facets
{
    using System;
    using System.Xml;

    /**
     * Work-around for Office2010 to accept the XAdES-BES/EPES signature.
     * 
     * xades:UnsignedProperties/xades:UnsignedSignatureProperties needs to be
     * present.
     * 
     * @author Frank Cornelis
     * 
     */
    public class Office2010SignatureFacet : SignatureFacet
    {


        public override void postSign(XmlDocument document)
        {
            // check for XAdES-BES
            XmlNodeList nl = document.GetElementsByTagName("QualifyingProperties", XADES_132_NS);
            if(nl.Count != 1)
            {
                throw new EncryptedDocumentException("no XAdES-BES extension present");
            }
            XmlElement qualProps = (XmlElement)nl.Item(0);

            // create basic XML Container structure
            XmlElement unsignedProps = GetOrAddChild(qualProps, "UnsignedProperties");
            GetOrAddChild(unsignedProps, "UnsignedSignatureProperties");
        }

        private static XmlElement GetOrAddChild(XmlElement parent, String localName)
        {
            foreach(XmlNode child in parent.ChildNodes)
            {
                if(child is XmlElement el && el.LocalName == localName && el.NamespaceURI == XADES_132_NS)
                {
                    return el;
                }
            }
            XmlElement newChild = parent.OwnerDocument.CreateElement(parent.Prefix, localName, XADES_132_NS);
            parent.AppendChild(newChild);
            return newChild;
        }
    }
}