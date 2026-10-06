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

namespace NPOI.POIFS.Crypt.Dsig
{
    using NPOI.OpenXml4Net.Exceptions;
    using NPOI.OpenXml4Net.OPC;
    using System;
    using System.IO;
    using System.Security.Cryptography.Xml;

    /**
     * URI dereferencer for Office Open XML documents: resolves manifest reference
     * URIs like "/word/document.xml?ContentType=..." to the package part data.
     */
    public class OOXMLURIDereferencer : IURIDereferencer, ISignatureConfigurable
    {
        private SignatureConfig signatureConfig;

        public void SetSignatureConfig(SignatureConfig signatureConfig)
        {
            this.signatureConfig = signatureConfig;
        }

        public IData dereference(IURIReference uriReference, SignedXml context)
        {
            if(null == uriReference)
            {
                throw new ArgumentNullException(nameof(uriReference), "URIReference cannot be null");
            }
            String uri = uriReference.getURI();
            Stream dataStream = Dereference(uri);
            if(dataStream == null)
            {
                throw new EncryptedDocumentException("cannot resolve uri: " + uri);
            }
            return new OctetStreamData(dataStream, uri);
        }

        /**
         * @return the data of the package part addressed by the uri, or null if there's no such part
         */
        public Stream Dereference(String uri)
        {
            PackagePart part = FindPart(uri);
            return part?.GetInputStream();
        }

        public PackagePart FindPart(String uri)
        {
            if(String.IsNullOrEmpty(uri))
            {
                return null;
            }

            int query = uri.IndexOf('?');
            String path = query >= 0 ? uri.Substring(0, query) : uri;
            if(path.Length == 0 || path[0] == '#')
            {
                return null;
            }
            if(path[0] != '/')
            {
                path = "/" + path;
            }

            OPCPackage pkg = signatureConfig.GetOpcPackage();
            PackagePart part = GetPart(pkg, path);
            if(part == null)
            {
                String unescaped = Uri.UnescapeDataString(path);
                if(unescaped != path)
                {
                    part = GetPart(pkg, unescaped);
                }
            }
            return part;
        }

        private static PackagePart GetPart(OPCPackage pkg, String path)
        {
            try
            {
                return pkg.GetPart(PackagingUriHelper.CreatePartName(path));
            }
            catch(InvalidFormatException)
            {
                return null;
            }
            catch(UriFormatException)
            {
                return null;
            }
        }
    }

    /**
     * Dereferenced octet stream data.
     */
    public class OctetStreamData : IData
    {
        public OctetStreamData(Stream octetStream, String uri)
        {
            OctetStream = octetStream;
            Uri = uri;
        }

        public Stream OctetStream { get; }

        public String Uri { get; }
    }
}