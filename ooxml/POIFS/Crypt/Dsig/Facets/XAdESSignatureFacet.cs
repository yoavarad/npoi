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
    using NPOI.POIFS.Crypt;
    using NPOI.POIFS.Crypt.Dsig.Services;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Numerics;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Cryptography.Xml;
    using System.Xml;


    /**
     * XAdES Signature Facet. Implements XAdES v1.4.1 which is compatible with XAdES
     * v1.3.2. The implemented XAdES format is XAdES-BES/EPES. It's up to another
     * part of the signature service to upgrade the XAdES-BES to a XAdES-X-L.
     * 
     * This implementation has been tested against an implementation that
     * participated multiple ETSI XAdES plugtests.
     * 
     * @author Frank Cornelis
     * @see <a href="http://en.wikipedia.org/wiki/XAdES">XAdES</a>
     * 
     */
    public class XAdESSignatureFacet : SignatureFacet
    {

        private static String XADES_TYPE = "http://uri.etsi.org/01903#SignedProperties";

        private Dictionary<String, String> dataObjectFormatMimeTypes = new Dictionary<String, String>();



        public override void preSign(
              XmlDocument document
            , List<Reference> references
            , List<XmlNode> objects)
        {
            // QualifyingProperties
            XmlElement qualifyingProperties = document.CreateElement("xd", "QualifyingProperties", XADES_132_NS);
            qualifyingProperties.SetAttribute("Target", "#" + signatureConfig.GetPackageSignatureId());

            // SignedProperties
            XmlElement signedProperties = AppendXades(qualifyingProperties, "SignedProperties");
            signedProperties.SetAttribute("Id", signatureConfig.GetXadesSignatureId());

            // SignedSignatureProperties
            XmlElement signedSignatureProperties = AppendXades(signedProperties, "SignedSignatureProperties");

            // SigningTime
            AppendXades(signedSignatureProperties, "SigningTime").InnerText =
                signatureConfig.GetExecutionTime().ToUniversalTime()
                    .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

            // SigningCertificate
            List<X509Certificate> chain = signatureConfig.GetSigningCertificateChain();
            if(chain == null || chain.Count == 0)
            {
                throw new EncryptedDocumentException("no signing certificate chain available");
            }
            XmlElement signingCertificate = AppendXades(signedSignatureProperties, "SigningCertificate");
            XmlElement certId = AppendXades(signingCertificate, "Cert");
            SetCertID(certId, signatureConfig, signatureConfig.IsXadesIssuerNameNoReverseOrder(), chain[0]);

            // ClaimedRole
            String role = signatureConfig.GetXadesRole();
            if(!String.IsNullOrEmpty(role))
            {
                XmlElement signerRole = AppendXades(signedSignatureProperties, "SignerRole");
                XmlElement claimedRoles = AppendXades(signerRole, "ClaimedRoles");
                AppendXades(claimedRoles, "ClaimedRole").InnerText = role;
            }

            // XAdES-EPES
            ISignaturePolicyService policyService = signatureConfig.GetSignaturePolicyService();
            if(policyService != null)
            {
                XmlElement signaturePolicyIdentifier = AppendXades(signedSignatureProperties, "SignaturePolicyIdentifier");
                XmlElement signaturePolicyId = AppendXades(signaturePolicyIdentifier, "SignaturePolicyId");

                XmlElement objectIdentifier = AppendXades(signaturePolicyId, "SigPolicyId");
                AppendXades(objectIdentifier, "Identifier").InnerText = policyService.GetSignaturePolicyIdentifier();
                String description = policyService.GetSignaturePolicyDescription();
                if(description != null)
                {
                    AppendXades(objectIdentifier, "Description").InnerText = description;
                }

                XmlElement sigPolicyHash = AppendXades(signaturePolicyId, "SigPolicyHash");
                SetDigestAlgAndValue(sigPolicyHash, policyService.GetSignaturePolicyDocument(), signatureConfig.GetDigestAlgo());

                String signaturePolicyDownloadUrl = policyService.GetSignaturePolicyDownloadUrl();
                if(null != signaturePolicyDownloadUrl)
                {
                    XmlElement sigPolicyQualifiers = AppendXades(signaturePolicyId, "SigPolicyQualifiers");
                    XmlElement sigPolicyQualifier = AppendXades(sigPolicyQualifiers, "SigPolicyQualifier");
                    AppendXades(sigPolicyQualifier, "SPURI").InnerText = signaturePolicyDownloadUrl;
                }
            }
            else if(signatureConfig.IsXadesSignaturePolicyImplied())
            {
                XmlElement signaturePolicyIdentifier = AppendXades(signedSignatureProperties, "SignaturePolicyIdentifier");
                AppendXades(signaturePolicyIdentifier, "SignaturePolicyImplied");
            }

            // DataObjectFormat
            if(dataObjectFormatMimeTypes.Count > 0)
            {
                XmlElement signedDataObjectProperties = AppendXades(signedProperties, "SignedDataObjectProperties");
                foreach(KeyValuePair<String, String> dataObjectFormatMimeType in dataObjectFormatMimeTypes)
                {
                    XmlElement dataObjectFormat = AppendXades(signedDataObjectProperties, "DataObjectFormat");
                    dataObjectFormat.SetAttribute("ObjectReference", "#" + dataObjectFormatMimeType.Key);
                    AppendXades(dataObjectFormat, "MimeType").InnerText = dataObjectFormatMimeType.Value;
                }
            }

            // add XAdES ds:Object
            XmlElement xadesObject = document.CreateElement("Object", XML_DIGSIG_NS);
            xadesObject.AppendChild(qualifyingProperties);
            objects.Add(xadesObject);

            // add XAdES ds:Reference
            List<Transform> transforms = new List<Transform>();
            transforms.Add(newTransform(signatureConfig.GetXadesCanonicalizationMethod()));
            Reference reference = newReference
                ("#" + signatureConfig.GetXadesSignatureId(), transforms, XADES_TYPE, null, null);
            references.Add(reference);
        }

        private static XmlElement AppendXades(XmlElement parent, String localName)
        {
            XmlElement child = parent.OwnerDocument.CreateElement("xd", localName, XADES_132_NS);
            parent.AppendChild(child);
            return child;
        }

        private static XmlElement AppendDsig(XmlElement parent, String localName)
        {
            XmlElement child = parent.OwnerDocument.CreateElement(localName, XML_DIGSIG_NS);
            parent.AppendChild(child);
            return child;
        }

        /**
         * Adds the ds:DigestMethod and ds:DigestValue elements to the given DigestAlgAndValue parent.
         *
         * @param digestAlgAndValue the parent for the new digest element
         * @param data the data to be digested
         * @param digestAlgo the digest algorithm
         */
        protected static void SetDigestAlgAndValue(
                XmlElement digestAlgAndValue,
                byte[] data,
                HashAlgorithm digestAlgo)
        {
            String digestMethodUri = SignatureConfig.GetDigestMethodUri(digestAlgo);
            AppendDsig(digestAlgAndValue, "DigestMethod").SetAttribute("Algorithm", digestMethodUri);
            AppendDsig(digestAlgAndValue, "DigestValue").InnerText =
                Convert.ToBase64String(DsigUtil.Digest(data, digestMethodUri));
        }

        /**
         * Fills the xades:Cert element (CertDigest and IssuerSerial) for the given certificate.
         */
        protected static void SetCertID
            (XmlElement certId, SignatureConfig signatureConfig, bool issuerNameNoReverseOrder, X509Certificate certificate)
        {
            XmlElement certDigest = AppendXades(certId, "CertDigest");
            SetDigestAlgAndValue(certDigest, certificate.GetRawCertData(), signatureConfig.GetXadesDigestAlgo());

            XmlElement issuerSerial = AppendXades(certId, "IssuerSerial");
            // .NET renders the issuer DN in the order of the certificate (RFC 2253 style separators)
            AppendDsig(issuerSerial, "X509IssuerName").InnerText = certificate.Issuer;
            AppendDsig(issuerSerial, "X509SerialNumber").InnerText = BigInteger.Parse(
                "0" + certificate.GetSerialNumberString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                .ToString(CultureInfo.InvariantCulture);
        }

        /**
         * Adds a mime-type for the given ds:Reference (referred via its @URI). This
         * information is Added via the xades:DataObjectFormat element.
         * 
         * @param dsReferenceUri
         * @param mimetype
         */
        public void AddMimeType(String dsReferenceUri, String mimetype)
        {
            this.dataObjectFormatMimeTypes.Add(dsReferenceUri, mimetype);
        }

        protected static void insertXChild(XmlNode root, XmlNode child)
        {
            root.AppendChild(child.OwnerDocument == root.OwnerDocument ? child : root.OwnerDocument.ImportNode(child, true));
        }

    }
}