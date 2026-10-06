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

namespace NPOI.POIFS.Crypt.Dsig
{
    using NPOI.POIFS.Crypt.Dsig.Services;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Cryptography.Xml;
    using System.Xml;
    using NpoiHashAlgorithm = NPOI.POIFS.Crypt.HashAlgorithm;

    /**
     * Internal helpers shared by the signing and validation code paths.
     */
    internal static class DsigUtil
    {
        internal const String C14N = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
        internal const String C14N_WITH_COMMENTS = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments";
        internal const String EXC_C14N = "http://www.w3.org/2001/10/xml-exc-c14n#";
        internal const String EXC_C14N_WITH_COMMENTS = "http://www.w3.org/2001/10/xml-exc-c14n#WithComments";
        internal const String ENVELOPED = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";

        /**
         * Parses xml without DTD processing or external entity resolution.
         */
        internal static XmlDocument LoadXml(Stream stream)
        {
            XmlReaderSettings settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            XmlDocument doc = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            using(XmlReader reader = XmlReader.Create(stream, settings))
            {
                doc.Load(reader);
            }
            return doc;
        }

        internal static XmlDocument LoadXml(String xml)
        {
            using(MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml)))
            {
                return LoadXml(ms);
            }
        }

        internal static Transform CreateTransform(String algorithm)
        {
            switch(algorithm)
            {
                case C14N:
                    return new XmlDsigC14NTransform(false);
                case C14N_WITH_COMMENTS:
                    return new XmlDsigC14NTransform(true);
                case EXC_C14N:
                    return new XmlDsigExcC14NTransform(false);
                case EXC_C14N_WITH_COMMENTS:
                    return new XmlDsigExcC14NTransform(true);
                case ENVELOPED:
                    return new XmlDsigEnvelopedSignatureTransform();
                case RelationshipTransformService.TRANSFORM_URI:
                    return new RelationshipTransformService();
                default:
                    throw new EncryptedDocumentException("unsupported transform: " + algorithm);
            }
        }

        internal static byte[] Canonicalize(XmlDocument doc, String algorithm)
        {
            Transform t = CreateTransform(algorithm ?? C14N);
            t.LoadInput(doc);
            using(Stream s = (Stream) t.GetOutput(typeof(Stream)))
            {
                return ReadAll(s);
            }
        }

        internal static System.Security.Cryptography.HashAlgorithm CreateHash(String digestMethodUri)
        {
            switch(digestMethodUri)
            {
                case "http://www.w3.org/2000/09/xmldsig#sha1":
#pragma warning disable CA5350 // SHA-1 is needed to verify and create legacy Office signatures
                    return SHA1.Create();
#pragma warning restore CA5350
                case "http://www.w3.org/2001/04/xmlenc#sha256":
                    return SHA256.Create();
                case "http://www.w3.org/2001/04/xmldsig-more#sha384":
                    return SHA384.Create();
                case "http://www.w3.org/2001/04/xmlenc#sha512":
                    return SHA512.Create();
                default:
                    throw new EncryptedDocumentException("unsupported digest method: " + digestMethodUri);
            }
        }

        internal static HashAlgorithmName GetHashAlgorithmName(NpoiHashAlgorithm hashAlgo)
        {
            if(hashAlgo == NpoiHashAlgorithm.sha1)
                return HashAlgorithmName.SHA1;
            if(hashAlgo == NpoiHashAlgorithm.sha256)
                return HashAlgorithmName.SHA256;
            if(hashAlgo == NpoiHashAlgorithm.sha384)
                return HashAlgorithmName.SHA384;
            if(hashAlgo == NpoiHashAlgorithm.sha512)
                return HashAlgorithmName.SHA512;
            throw new EncryptedDocumentException("Hash algorithm " + hashAlgo?.jceId + " not supported for signing.");
        }

        internal static byte[] Digest(byte[] data, String digestMethodUri)
        {
            using(System.Security.Cryptography.HashAlgorithm md = CreateHash(digestMethodUri))
            {
                return md.ComputeHash(data);
            }
        }

        /**
         * Applies the reference transforms to the dereferenced octets and digests the result.
         */
        internal static byte[] DigestReference(Stream data, IList<Transform> transforms, String digestMethodUri)
        {
            object current = ReadAll(data);
            if(transforms != null)
            {
                foreach(Transform t in transforms)
                {
                    XmlDocument doc = current as XmlDocument ?? LoadXml(new MemoryStream((byte[])current));
                    if(t is RelationshipTransformService rt)
                    {
                        current = RelationshipTransformService.Transform(doc, rt.SourceIds, rt.SourceTypes);
                    }
                    else if(t is XmlDsigC14NTransform || t is XmlDsigExcC14NTransform)
                    {
                        t.LoadInput(doc);
                        using(Stream s = (Stream) t.GetOutput(typeof(Stream)))
                        {
                            current = ReadAll(s);
                        }
                    }
                    else
                    {
                        throw new EncryptedDocumentException("unsupported manifest transform: " + t.Algorithm);
                    }
                }
            }
            byte[] octets = current as byte[] ?? Canonicalize((XmlDocument)current, C14N);
            return Digest(octets, digestMethodUri);
        }

        internal static byte[] ReadAll(Stream s)
        {
            if(s is MemoryStream ms)
            {
                return ms.ToArray();
            }
            using(MemoryStream bos = new MemoryStream())
            {
                s.CopyTo(bos);
                return bos.ToArray();
            }
        }
    }
}