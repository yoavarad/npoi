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
    using NPOI.OpenXml4Net.OPC;
    using NPOI.POIFS.Crypt.Dsig.Facets;
    using NPOI.POIFS.Crypt.Dsig.Services;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Cryptography.Xml;
    using System.Text;
    using System.Xml;

    /**
     * <p>This class is the default entry point for XML signatures and can be used for
     * validating an existing signed office document and signing a office document.</p>
     *
     * <p><b>Validating a signed office document</b></p>
     *
     * <pre>
     * OPCPackage pkg = OPCPackage.Open(..., PackageAccess.READ);
     * SignatureConfig sic = new SignatureConfig();
     * sic.SetOpcPackage(pkg);
     * SignatureInfo si = new SignatureInfo();
     * si.SetSignatureConfig(sic);
     * bool isValid = si.VerifySignature();
     * ...
     * </pre>
     *
     * <p><b>Signing an office document</b></p>
     *
     * <pre>
     * X509Certificate2 x509 = new X509Certificate2("test.pfx", "test", X509KeyStorageFlags.Exportable);
     * SignatureConfig signatureConfig = new SignatureConfig();
     * signatureConfig.SetKey(x509.GetRSAPrivateKey());
     * signatureConfig.SetSigningCertificateChain(new List&lt;X509Certificate&gt; { x509 });
     * OPCPackage pkg = OPCPackage.Open(..., PackageAccess.READ_WRITE);
     * signatureConfig.SetOpcPackage(pkg);
     *
     * // adding the signature document to the package
     * SignatureInfo si = new SignatureInfo();
     * si.SetSignatureConfig(signatureConfig);
     * si.ConfirmSignature();
     * // optionally verify the generated signature
     * bool b = si.VerifySignature();
     * // write the changes back to disc
     * pkg.Close();
     * </pre>
     *
     * <p><b>Implementation notes:</b></p>
     *
     * <p>The XML signature handling is based on System.Security.Cryptography.Xml.
     * The package part references of the ds:Manifest are digested and validated
     * by this class, as SignedXml doesn't process manifests.
     * Currently only RSA keys are supported for signing.</p>
     */
    public class SignatureInfo : ISignatureConfigurable
    {
        protected internal SignatureConfig signatureConfig;

        public class SignaturePart
        {
            private readonly PackagePart signaturePart;
            private readonly SignatureConfig signatureConfig;
            private X509Certificate signer;
            private List<X509Certificate> certChain;

            internal SignaturePart(PackagePart signaturePart, SignatureConfig signatureConfig)
            {
                this.signaturePart = signaturePart;
                this.signatureConfig = signatureConfig;
            }

            /**
             * @return the package part Containing the signature
             */
            public PackagePart GetPackagePart()
            {
                return signaturePart;
            }

            /**
             * @return the signer certificate
             */
            public X509Certificate GetSigner()
            {
                return signer;
            }

            /**
             * @return the certificate chain of the signer
             */
            public List<X509Certificate> GetCertChain()
            {
                return certChain;
            }

            /**
             * Helper method for examining the xml signature
             *
             * @return the xml signature document
             */
            public XmlDocument GetSignatureDocument()
            {
                using(Stream stream = signaturePart.GetInputStream())
                {
                    return DsigUtil.LoadXml(stream);
                }
            }

            /**
             * @return true, when the xml signature is valid, false otherwise
             *
             * @throws EncryptedDocumentException if the signature can't be extracted or if its malformed
             */
            public bool Validate()
            {
                try
                {
                    XmlDocument doc = GetSignatureDocument();
                    SignedXml signedXml = new SignedXml(doc);
                    // no external resolution of reference uris
                    signedXml.Resolver = null;
                    signedXml.LoadXml(doc.DocumentElement);
                    foreach(Reference reference in signedXml.SignedInfo.References)
                    {
                        String refUri = reference.Uri;
                        if(!String.IsNullOrEmpty(refUri) && refUri[0] != '#')
                        {
                            // only same-document references are allowed in the SignedInfo
                            return false;
                        }
                    }

                    List<X509Certificate2> certificates = new List<X509Certificate2>();
                    foreach(KeyInfoClause clause in signedXml.KeyInfo)
                    {
                        if(clause is KeyInfoX509Data x509Data && x509Data.Certificates != null)
                        {
                            foreach(X509Certificate cert in x509Data.Certificates)
                            {
                                certificates.Add(cert as X509Certificate2 ?? new X509Certificate2(cert));
                            }
                        }
                    }

                    X509Certificate2 signerCert = null;
                    foreach(X509Certificate2 cert in certificates)
                    {
                        if(signedXml.CheckSignature(cert, true) || CheckLegacySignature(signedXml, cert))
                        {
                            signerCert = cert;
                            break;
                        }
                    }

                    if(signerCert == null || !ValidateManifests(doc, signedXml))
                    {
                        return false;
                    }

                    signer = signerCert;
                    certChain = certificates.ConvertAll(c => (X509Certificate) c);
                    return true;
                }
                catch(Exception e) when(e is XmlException || e is CryptographicException || e is IOException
                    || e is FormatException || e is ArgumentException || e is NPOI.OpenXml4Net.Exceptions.InvalidFormatException)
                {
                    throw new EncryptedDocumentException("error in marshalling and validating the signature", e);
                }
            }

            /**
             * Retries the validation with a key that also accepts the legacy DigestInfo
             * encoding (without NULL parameters), which Windows accepts natively.
             */
            private static bool CheckLegacySignature(SignedXml signedXml, X509Certificate2 cert)
            {
                using(RSA rsa = cert.GetRSAPublicKey())
                {
                    if(rsa == null)
                    {
                        return false;
                    }
                    using(LegacyDigestInfoRsa legacyRsa = new LegacyDigestInfoRsa(rsa))
                    {
                        return signedXml.CheckSignature(legacyRsa);
                    }
                }
            }

            /**
             * Checks the digests of the package part references of all manifests
             * which are contained in a signed ds:Object.
             */
            private bool ValidateManifests(XmlDocument doc, SignedXml signedXml)
            {
                HashSet<String> signedObjectIds = new HashSet<String>();
                foreach(Reference reference in signedXml.SignedInfo.References)
                {
                    String uri = reference.Uri;
                    if(uri != null && uri.Length > 1 && uri[0] == '#')
                    {
                        signedObjectIds.Add(uri.Substring(1));
                    }
                }

                OOXMLURIDereferencer dereferencer = new OOXMLURIDereferencer();
                dereferencer.SetSignatureConfig(signatureConfig);

                foreach(XmlElement manifest in doc.GetElementsByTagName("Manifest", SignatureFacet.XML_DIGSIG_NS))
                {
                    if(!IsInSignedObject(manifest, signedObjectIds))
                    {
                        continue;
                    }
                    foreach(XmlNode node in manifest.ChildNodes)
                    {
                        if(node is XmlElement reference
                            && reference.LocalName == "Reference"
                            && reference.NamespaceURI == SignatureFacet.XML_DIGSIG_NS
                            && !ValidateManifestReference(reference, dereferencer))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            private static bool IsInSignedObject(XmlElement manifest, HashSet<String> signedObjectIds)
            {
                for(XmlNode n = manifest.ParentNode; n is XmlElement el; n = n.ParentNode)
                {
                    if(el.LocalName == "Object"
                        && el.NamespaceURI == SignatureFacet.XML_DIGSIG_NS
                        && signedObjectIds.Contains(el.GetAttribute("Id")))
                    {
                        return true;
                    }
                }
                return false;
            }

            private static bool ValidateManifestReference(XmlElement reference, OOXMLURIDereferencer dereferencer)
            {
                String uri = reference.GetAttribute("URI");
                String digestMethod = null;
                String digestValue = null;
                List<Transform> transforms = new List<Transform>();
                foreach(XmlNode node in reference.ChildNodes)
                {
                    if(!(node is XmlElement el) || el.NamespaceURI != SignatureFacet.XML_DIGSIG_NS)
                    {
                        continue;
                    }
                    switch(el.LocalName)
                    {
                        case "DigestMethod":
                            digestMethod = el.GetAttribute("Algorithm");
                            break;
                        case "DigestValue":
                            digestValue = el.InnerText;
                            break;
                        case "Transforms":
                            foreach(XmlNode tn in el.ChildNodes)
                            {
                                if(tn is XmlElement te && te.LocalName == "Transform" && te.NamespaceURI == SignatureFacet.XML_DIGSIG_NS)
                                {
                                    Transform t = DsigUtil.CreateTransform(te.GetAttribute("Algorithm"));
                                    if(t is RelationshipTransformService)
                                    {
                                        t.LoadInnerXml(te.ChildNodes);
                                    }
                                    transforms.Add(t);
                                }
                            }
                            break;
                    }
                }

                if(digestMethod == null || digestValue == null)
                {
                    return false;
                }

                PackagePart part = dereferencer.FindPart(uri);
                if(part == null)
                {
                    // referenced part is missing
                    return false;
                }
                int query = uri.IndexOf("?ContentType=", StringComparison.Ordinal);
                if(query >= 0 && !String.Equals(uri.Substring(query + "?ContentType=".Length), part.ContentType, StringComparison.OrdinalIgnoreCase))
                {
                    // the content type is part of the signed reference
                    return false;
                }
                using(Stream data = part.GetInputStream())
                {
                    byte[] actual = DsigUtil.DigestReference(data, transforms, digestMethod);
                    byte[] expected = Convert.FromBase64String(digestValue.Trim());
                    return CryptographicEquals(actual, expected);
                }
            }

            private static bool CryptographicEquals(byte[] a, byte[] b)
            {
                if(a.Length != b.Length)
                {
                    return false;
                }
                int diff = 0;
                for(int i = 0; i < a.Length; i++)
                {
                    diff |= a[i] ^ b[i];
                }
                return diff == 0;
            }
        }

        /**
         * Constructor Initializes xml signature environment, if it hasn't been Initialized before
         */
        public SignatureInfo()
        {
            InitXmlProvider();
        }

        /**
         * @return the signature config
         */
        public SignatureConfig GetSignatureConfig()
        {
            return signatureConfig;
        }

        /**
         * @param signatureConfig the signature config, needs to be Set before a SignatureInfo object is used
         */
        public void SetSignatureConfig(SignatureConfig signatureConfig)
        {
            this.signatureConfig = signatureConfig;
        }

        /**
         * @return true, if first signature part is valid
         */
        public bool VerifySignature()
        {
            foreach(SignaturePart sp in GetSignatureParts())
            {
                // only validate first part
                return sp.Validate();
            }
            return false;
        }

        /**
         * add the xml signature to the document
         */
        public void ConfirmSignature()
        {
            XmlDocument document = new XmlDocument();

            // operate
            DigestInfo digestInfo = preSign(document, null);

            // Setup: key material, signature value
            byte[] signatureValue = signDigest(digestInfo.digestValue);

            // operate: postSign
            postSign(document, signatureValue);
        }

        /**
         * Sign (encrypt) the digest with the private key.
         * Currently only rsa is supported.
         *
         * @param digest the hashed input
         * @return the encrypted hash
         */
        public byte[] signDigest(byte[] digest)
        {
            if(!(signatureConfig.GetKey() is RSA rsa))
            {
                throw new EncryptedDocumentException("only RSA private keys are supported for signing");
            }
            try
            {
                return rsa.SignHash(digest, DsigUtil.GetHashAlgorithmName(signatureConfig.GetDigestAlgo()), RSASignaturePadding.Pkcs1);
            }
            catch(CryptographicException e)
            {
                throw new EncryptedDocumentException("unable to sign the digest", e);
            }
        }

        /**
         * @return a signature part for each signature document.
         * the parts can be validated independently.
         */
        public IEnumerable<SignaturePart> GetSignatureParts()
        {
            signatureConfig.Init(true);
            OPCPackage pkg = signatureConfig.GetOpcPackage();
            foreach(PackageRelationship sigOrigRel in pkg.GetRelationshipsByType(PackageRelationshipTypes.DIGITAL_SIGNATURE_ORIGIN))
            {
                PackagePart sigPart = pkg.GetPart(sigOrigRel);
                if(sigPart == null)
                {
                    continue;
                }
                foreach(PackageRelationship sigRel in sigPart.GetRelationshipsByType(PackageRelationshipTypes.DIGITAL_SIGNATURE))
                {
                    PackagePart sigRelPart = sigPart.GetRelatedPart(sigRel);
                    if(sigRelPart != null)
                    {
                        yield return new SignaturePart(sigRelPart, signatureConfig);
                    }
                }
            }
        }

        /**
         * Initialize the xml signing environment.
         * Nothing to do on .NET, kept for API compatibility.
         */
        protected static void InitXmlProvider()
        {
        }

        /**
         * Helper method for Adding informations before the signing.
         * Normally {@link #ConfirmSignature()} is sufficient to be used.
         *
         * @param document an empty document, which will contain the ds:Signature afterwards
         * @param digestInfos external digests - not supported, needs to be null or empty
         * @return the digest of the canonicalized ds:SignedInfo, which needs to be signed
         */
        public DigestInfo preSign(XmlDocument document, List<DigestInfo> digestInfos)
        {
            signatureConfig.Init(false);

            if(digestInfos != null && digestInfos.Count > 0)
            {
                throw new NotSupportedException("signing of external digests is not supported");
            }

            /*
             * Build the ds:Signature skeleton and let the signature facets add
             * their references and objects.
             */
            XmlDocument skeleton = new XmlDocument { PreserveWhitespace = true };
            XmlElement signatureElement = skeleton.CreateElement("Signature", SignatureFacet.XML_DIGSIG_NS);
            signatureElement.SetAttribute("Id", signatureConfig.GetPackageSignatureId());
            skeleton.AppendChild(signatureElement);

            List<Reference> references = new List<Reference>();
            List<XmlNode> objects = new List<XmlNode>();
            foreach(SignatureFacet signatureFacet in signatureConfig.GetSignatureFacets())
            {
                signatureFacet.preSign(skeleton, references, objects);
            }
            foreach(XmlNode xo in objects)
            {
                signatureElement.AppendChild(xo.OwnerDocument == skeleton ? xo : skeleton.ImportNode(xo, true));
            }

            // re-parse, so the digests are calculated on the same infoset as the validation
            document.PreserveWhitespace = true;
            document.XmlResolver = null;
            document.LoadXml(skeleton.OuterXml);

            /*
             * Calculate the reference digests. SignedXml only exposes this as part of the
             * signature calculation, so a throw-away MAC is used and the ds:SignatureMethod
             * is replaced afterwards.
             */
            SignedXml signedXml = new SignedXml(document);
            signedXml.SignedInfo.CanonicalizationMethod = signatureConfig.GetCanonicalizationMethod();
            foreach(Reference reference in references)
            {
                signedXml.AddReference(reference);
            }
            using(HMACSHA256 mac = new HMACSHA256(new byte[32]))
            {
                signedXml.ComputeSignature(mac);
            }

            XmlElement signedInfo = (XmlElement)document.ImportNode(signedXml.SignedInfo.GetXml(), true);
            XmlElement signatureMethod = (XmlElement)signedInfo.GetElementsByTagName("SignatureMethod", SignatureFacet.XML_DIGSIG_NS)[0];
            signatureMethod.SetAttribute("Algorithm", signatureConfig.GetSignatureMethodUri());

            XmlElement signatureValue = document.CreateElement("SignatureValue", SignatureFacet.XML_DIGSIG_NS);
            signatureValue.SetAttribute("Id", signatureConfig.GetPackageSignatureId() + "-signature-value");

            XmlElement root = document.DocumentElement;
            root.InsertBefore(signatureValue, root.FirstChild);
            root.InsertBefore(signedInfo, signatureValue);

            /*
             * Calculation of XML signature digest value.
             */
            byte[] octets = DsigUtil.Canonicalize(DsigUtil.LoadXml(signedInfo.OuterXml), signatureConfig.GetCanonicalizationMethod());
            byte[] digestValue = DsigUtil.Digest(octets, signatureConfig.GetDigestMethodUri());

            String description = signatureConfig.GetSignatureDescription();
            return new DigestInfo(digestValue, signatureConfig.GetDigestAlgo(), description);
        }

        /**
         * Helper method for Adding informations After the signing.
         * Normally {@link #ConfirmSignature()} is sufficient to be used.
         */
        public void postSign(XmlDocument document, byte[] signatureValue)
        {
            /*
             * Check ds:Signature node.
             */
            String signatureId = signatureConfig.GetPackageSignatureId();
            if(document.DocumentElement == null || !signatureId.Equals(document.DocumentElement.GetAttribute("Id")))
            {
                throw new EncryptedDocumentException("ds:Signature not found for @Id: " + signatureId);
            }

            /*
             * Insert signature value into the ds:SignatureValue element
             */
            XmlNodeList sigValNl = document.GetElementsByTagName("SignatureValue", SignatureFacet.XML_DIGSIG_NS);
            if(sigValNl.Count != 1)
            {
                throw new EncryptedDocumentException("preSign has to be called before postSign");
            }
            sigValNl.Item(0).InnerText = Convert.ToBase64String(signatureValue);

            /*
             * Allow signature facets to inject their own stuff.
             */
            foreach(SignatureFacet signatureFacet in signatureConfig.GetSignatureFacets())
            {
                signatureFacet.postSign(document);
            }

            WriteDocument(document);
        }

        /**
         * Write XML signature into the OPC package
         *
         * @param document the xml signature document
         */
        protected void WriteDocument(XmlDocument document)
        {
            /*
             * Copy the original OOXML content to the signed OOXML package. During
             * copying some files need to Changed.
             */
            OPCPackage pkg = signatureConfig.GetOpcPackage();

            // <Default Extension="sigs" ContentType="application/vnd.openxmlformats-package.digital-signature-origin"/>
            PackagePartName sigsPartName = PackagingUriHelper.CreatePartName("/_xmlsignatures/origin.sigs");
            PackagePart sigsPart = pkg.GetPart(sigsPartName);
            if(sigsPart == null)
            {
                // touch empty marker file
                sigsPart = pkg.CreatePart(sigsPartName, ContentTypes.DIGITAL_SIGNATURE_ORIGIN_PART);
            }

            // <Override PartName="/_xmlsignatures/sig1.xml" ContentType="application/vnd.openxmlformats-package.digital-signature-xmlsignature+xml"/>
            PackagePartName sigPartName;
            int sigIndex = 1;
            do
            {
                sigPartName = PackagingUriHelper.CreatePartName("/_xmlsignatures/sig" + sigIndex++ + ".xml");
            } while(pkg.GetPart(sigPartName) != null);

            PackagePart sigPart = pkg.CreatePart(sigPartName, ContentTypes.DIGITAL_SIGNATURE_XML_SIGNATURE_PART);
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = false
            };
            using(Stream os = sigPart.GetOutputStream())
            using(XmlWriter writer = XmlWriter.Create(os, settings))
            {
                document.Save(writer);
            }

            PackageRelationshipCollection relCol = pkg.GetRelationshipsByType(PackageRelationshipTypes.DIGITAL_SIGNATURE_ORIGIN);
            if(relCol.Size == 0)
            {
                pkg.AddRelationship(sigsPartName, TargetMode.Internal, PackageRelationshipTypes.DIGITAL_SIGNATURE_ORIGIN);
            }

            sigsPart.AddRelationship(sigPartName, TargetMode.Internal, PackageRelationshipTypes.DIGITAL_SIGNATURE);
        }

        /**
         * Helper method for null lists, which are Converted to empty lists
         *
         * @param other the reference to wrap, if null
         * @return if other is null, an empty lists is returned, otherwise other is returned
         */
        private static List<T> safe<T>(List<T> other)
        {
            return other ?? new List<T>();
        }
    }

}