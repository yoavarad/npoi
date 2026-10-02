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
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Cryptography.Xml;
    using System.Xml;

    /**
* Signature Facet implementation that Adds ds:KeyInfo to the XML signature.
* 
* @author Frank Cornelis
* 
*/
    public class KeyInfoSignatureFacet : SignatureFacet
    {


        public override void postSign(XmlDocument document)
        {
            XmlNodeList nl = document.GetElementsByTagName("Object", XML_DIGSIG_NS);

            /*
             * Make sure we insert right After the ds:SignatureValue element, just
             * before the first ds:Object element.
             */
            XmlNode nextSibling = (nl.Count == 0) ? null : nl.Item(0);

            List<X509Certificate> chain = signatureConfig.GetSigningCertificateChain();
            if(chain == null || chain.Count == 0)
            {
                throw new EncryptedDocumentException("no signing certificate chain available");
            }
            X509Certificate signingCertificate = chain[0];

            KeyInfo keyInfo = new KeyInfo();

            if(signatureConfig.IsIncludeKeyValue())
            {
                RSA publicKey = new X509Certificate2(signingCertificate).GetRSAPublicKey();
                if(publicKey == null)
                {
                    throw new EncryptedDocumentException("only RSA keys are supported for the key value");
                }
                keyInfo.AddClause(new RSAKeyValue(publicKey));
            }

            KeyInfoX509Data x509Data = new KeyInfoX509Data();
            if(signatureConfig.IsIncludeIssuerSerial())
            {
                x509Data.AddIssuerSerial(signingCertificate.Issuer, signingCertificate.GetSerialNumberString());
            }

            if(signatureConfig.IsIncludeEntireCertificateChain())
            {
                foreach(X509Certificate certificate in chain)
                {
                    x509Data.AddCertificate(certificate);
                }
            }
            else
            {
                x509Data.AddCertificate(signingCertificate);
            }
            keyInfo.AddClause(x509Data);

            XmlNode keyInfoElement = document.ImportNode(keyInfo.GetXml(), true);
            document.DocumentElement.InsertBefore(keyInfoElement, nextSibling);
        }
    }
}