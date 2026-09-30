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

namespace TestCases.POIFS.Crypt
{
    using ICSharpCode.SharpZipLib.Zip;
    using NPOI.OpenXml4Net.OPC;
    using NPOI.POIFS.Crypt;
    using NPOI.POIFS.Crypt.Dsig;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Xml;
    using TestCases;
    using HashAlgorithm = NPOI.POIFS.Crypt.HashAlgorithm;

    [TestFixture]
    public class TestSignatureInfo
    {
        private static readonly POIDataSamples testdata = POIDataSamples.GetXmlDSignInstance();

        private static readonly DateTime executionTime = new DateTime(2014, 8, 6, 21, 42, 12, DateTimeKind.Utc);

        [Test]
        public void office2007prettyPrintedRels()
        {
            OPCPackage pkg = OPCPackage.Open(testdata.GetFileInfo("office2007prettyPrintedRels.docx"), PackageAccess.READ);
            try
            {
                SignatureInfo si = NewSignatureInfo(pkg);
                ClassicAssert.IsTrue(si.VerifySignature());
            }
            finally
            {
                pkg.Revert();
            }
        }

        [Test]
        public void GetSignerUnsigned()
        {
            String[] testFiles = {
                "hello-world-unsigned.docx",
                "hello-world-unsigned.pptx",
                "hello-world-unsigned.xlsx",
                "hello-world-office-2010-technical-preview-unsigned.docx"
            };

            foreach(String testFile in testFiles)
            {
                OPCPackage pkg = OPCPackage.Open(testdata.GetFileInfo(testFile), PackageAccess.READ);
                try
                {
                    SignatureInfo si = NewSignatureInfo(pkg);
                    ClassicAssert.IsEmpty(GetValidSigners(si), "test-file: " + testFile);
                    ClassicAssert.IsFalse(si.VerifySignature(), "test-file: " + testFile);
                }
                finally
                {
                    pkg.Revert();
                }
            }
        }

        [TestCase("hyperlink-example-signed.docx")]
        [TestCase("hello-world-signed.docx")]
        [TestCase("hello-world-signed.pptx")]
        [TestCase("hello-world-signed.xlsx")]
        [TestCase("hello-world-office-2010-technical-preview.docx")]
        [TestCase("ms-office-2010-signed.docx")]
        [TestCase("ms-office-2010-signed.pptx")]
        [TestCase("ms-office-2010-signed.xlsx")]
        [TestCase("Office2010-SP1-XAdES-X-L.docx")]
        [TestCase("signed.docx")]
        public void GetSigner(String testFile)
        {
            OPCPackage pkg = OPCPackage.Open(testdata.GetFileInfo(testFile), PackageAccess.READ);
            try
            {
                SignatureInfo si = NewSignatureInfo(pkg);
                List<X509Certificate> result = GetValidSigners(si);

                ClassicAssert.AreEqual(1, result.Count, "test-file: " + testFile);
                ClassicAssert.IsNotNull(result[0]);
                ClassicAssert.IsTrue(si.VerifySignature(), "test-file: " + testFile);
            }
            finally
            {
                pkg.Revert();
            }
        }

        [Test]
        public void GetMultiSigners()
        {
            String testFile = "hello-world-signed-twice.docx";
            OPCPackage pkg = OPCPackage.Open(testdata.GetFileInfo(testFile), PackageAccess.READ);
            try
            {
                SignatureInfo si = NewSignatureInfo(pkg);
                List<X509Certificate> result = GetValidSigners(si);

                ClassicAssert.AreEqual(2, result.Count, "test-file: " + testFile);
                ClassicAssert.AreNotEqual(result[0].Subject, result[1].Subject);
                ClassicAssert.IsTrue(si.VerifySignature(), "test-file: " + testFile);
            }
            finally
            {
                pkg.Revert();
            }
        }

        [TestCase("hello-world-unsigned.docx")]
        [TestCase("hello-world-unsigned.xlsx")]
        [TestCase("hello-world-unsigned.pptx")]
        public void SignAndVerify(String testFile)
        {
            using(RSA key = CreateKey())
            {
                X509Certificate2 cert = CreateCertificate("CN=Test", key);
                byte[] signedBytes = SignFixture(testFile, key, new List<X509Certificate> { cert }, HashAlgorithm.sha1);

                OPCPackage pkg = OPCPackage.Open(new MemoryStream(signedBytes));
                try
                {
                    SignatureInfo si = NewSignatureInfo(pkg);
                    List<X509Certificate> result = GetValidSigners(si);
                    ClassicAssert.AreEqual(1, result.Count);
                    ClassicAssert.AreEqual(cert.Subject, result[0].Subject);
                    ClassicAssert.IsTrue(si.VerifySignature());
                }
                finally
                {
                    pkg.Revert();
                }
            }
        }

        [Test]
        public void TestSignSpreadsheetWithSignatureInfo()
        {
            // validate the signature within the same, not yet saved package
            using(RSA key = CreateKey())
            {
                X509Certificate2 cert = CreateCertificate("CN=Test", key);
                OPCPackage pkg = OPCPackage.Open(testdata.OpenResourceAsStream("hello-world-unsigned.xlsx"));
                try
                {
                    SignatureConfig sic = NewSigningConfig(pkg, key, new List<X509Certificate> { cert }, HashAlgorithm.sha1);
                    SignatureInfo si = new SignatureInfo();
                    si.SetSignatureConfig(sic);
                    si.ConfirmSignature();

                    ClassicAssert.AreEqual(1, GetValidSigners(si).Count);
                }
                finally
                {
                    pkg.Revert();
                }
            }
        }

        [Test]
        public void TestSignatureContent()
        {
            using(RSA key = CreateKey())
            {
                X509Certificate2 cert = CreateCertificate("CN=Test", key);
                byte[] signedBytes = SignFixture("hello-world-unsigned.docx", key, new List<X509Certificate> { cert }, HashAlgorithm.sha1);

                XmlDocument sig = LoadSignatureXml(signedBytes, "_xmlsignatures/sig1.xml");
                XmlNamespaceManager ns = new XmlNamespaceManager(sig.NameTable);
                ns.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
                ns.AddNamespace("xd", "http://uri.etsi.org/01903/v1.3.2#");
                ns.AddNamespace("mdssi", "http://schemas.openxmlformats.org/package/2006/digital-signature");

                ClassicAssert.AreEqual("idPackageSignature", sig.DocumentElement.GetAttribute("Id"));
                ClassicAssert.IsNotNull(sig.SelectSingleNode("/ds:Signature/ds:SignedInfo/ds:Reference[@URI='#idPackageObject']", ns));
                ClassicAssert.IsNotNull(sig.SelectSingleNode("/ds:Signature/ds:SignedInfo/ds:Reference[@URI='#idOfficeObject']", ns));
                ClassicAssert.IsNotNull(sig.SelectSingleNode("/ds:Signature/ds:SignedInfo/ds:Reference[@URI='#idSignedProperties']", ns));
                ClassicAssert.IsNotNull(sig.SelectSingleNode("//ds:Manifest/ds:Reference[starts-with(@URI, '/word/document.xml?ContentType=')]", ns));
                ClassicAssert.IsNotNull(sig.SelectSingleNode("//ds:Manifest/ds:Reference[starts-with(@URI, '/_rels/.rels?')]/ds:Transforms/ds:Transform/mdssi:RelationshipReference", ns));
                ClassicAssert.AreEqual("2014-08-06T21:42:12Z", sig.SelectSingleNode("//mdssi:SignatureTime/mdssi:Value", ns).InnerText);
                ClassicAssert.AreEqual("2014-08-06T21:42:12Z", sig.SelectSingleNode("//xd:SigningTime", ns).InnerText);
                ClassicAssert.IsNotNull(sig.SelectSingleNode("//xd:QualifyingProperties/xd:UnsignedProperties/xd:UnsignedSignatureProperties", ns));
                ClassicAssert.IsNotNull(sig.SelectSingleNode("/ds:Signature/ds:KeyInfo/ds:X509Data/ds:X509Certificate", ns));
            }
        }

        [Test]
        public void TestIncludeIssuerSerialAndKeyValue()
        {
            using(RSA key = CreateKey())
            {
                X509Certificate2 cert = CreateCertificate("CN=Test", key);
                OPCPackage pkg = OPCPackage.Open(testdata.OpenResourceAsStream("hello-world-unsigned.docx"));
                try
                {
                    SignatureConfig sic = NewSigningConfig(pkg, key, new List<X509Certificate> { cert }, HashAlgorithm.sha256);
                    sic.SetIncludeIssuerSerial(true);
                    sic.SetIncludeKeyValue(true);
                    SignatureInfo si = new SignatureInfo();
                    si.SetSignatureConfig(sic);
                    si.ConfirmSignature();
                    ClassicAssert.IsTrue(si.VerifySignature());
                }
                finally
                {
                    pkg.Revert();
                }
            }
        }

        [Test]
        public void TestManipulation()
        {
            using(RSA key = CreateKey())
            {
                X509Certificate2 cert = CreateCertificate("CN=Test", key);
                byte[] signedBytes = SignFixture("hello-world-unsigned.xlsx", key, new List<X509Certificate> { cert }, HashAlgorithm.sha1);

                // manipulate a signed part
                byte[] manipulated = ReplaceInEntry(signedBytes, "xl/workbook.xml", "name=\"Sheet1\"", "name=\"manipulated\"");

                OPCPackage pkg = OPCPackage.Open(new MemoryStream(manipulated));
                try
                {
                    SignatureInfo si = NewSignatureInfo(pkg);
                    ClassicAssert.IsFalse(si.VerifySignature(), "signature should be broken");
                    ClassicAssert.IsEmpty(GetValidSigners(si));
                }
                finally
                {
                    pkg.Revert();
                }
            }
        }

        [Test]
        public void TestManipulatedOfficeFixture()
        {
            byte[] original = File.ReadAllBytes(testdata.GetFileInfo("hello-world-signed.docx").FullName);
            byte[] manipulated = ReplaceInEntry(original, "word/document.xml", "Hello world", "Hello there");

            OPCPackage pkg = OPCPackage.Open(new MemoryStream(manipulated));
            try
            {
                ClassicAssert.IsFalse(NewSignatureInfo(pkg).VerifySignature(), "signature should be broken");
            }
            finally
            {
                pkg.Revert();
            }
        }

        [Test]
        public void TestManipulatedSignature()
        {
            using(RSA key = CreateKey())
            {
                X509Certificate2 cert = CreateCertificate("CN=Test", key);
                byte[] signedBytes = SignFixture("hello-world-unsigned.docx", key, new List<X509Certificate> { cert }, HashAlgorithm.sha1);

                // manipulate a signed property of the signature itself
                byte[] manipulated = ReplaceInEntry(signedBytes, "_xmlsignatures/sig1.xml", "2014-08-06T21:42:12Z", "2015-08-06T21:42:12Z");

                OPCPackage pkg = OPCPackage.Open(new MemoryStream(manipulated));
                try
                {
                    ClassicAssert.IsFalse(NewSignatureInfo(pkg).VerifySignature(), "signature should be broken");
                }
                finally
                {
                    pkg.Revert();
                }
            }
        }

        [Ignore("XAdES-XL (timestamp and revocation data) is not implemented")]
        public void TestSignEnvelopingDocument()
        {
        }

        [Test]
        public void TestCertChain()
        {
            using(RSA caKey = CreateKey())
            using(RSA key = CreateKey())
            {
                X509Certificate2 caCert = CreateCertificate("CN=CA", caKey, true);
                CertificateRequest req = new CertificateRequest("CN=Test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
                X509Certificate2 cert = req.Create(caCert, caCert.NotBefore, caCert.NotAfter, new byte[] { 1, 2, 3, 4 });

                byte[] signedBytes = SignFixture("hello-world-unsigned.xlsx", key, new List<X509Certificate> { cert, caCert }, HashAlgorithm.sha1);

                OPCPackage pkg = OPCPackage.Open(new MemoryStream(signedBytes));
                try
                {
                    SignatureInfo si = NewSignatureInfo(pkg);
                    int count = 0;
                    foreach(SignatureInfo.SignaturePart sp in si.GetSignatureParts())
                    {
                        ClassicAssert.IsTrue(sp.Validate());
                        ClassicAssert.AreEqual(cert.Subject, sp.GetSigner().Subject);
                        ClassicAssert.AreEqual(2, sp.GetCertChain().Count);
                        count++;
                    }
                    ClassicAssert.AreEqual(1, count);
                }
                finally
                {
                    pkg.Revert();
                }
            }
        }

        [Test]
        public void TestNonSha1()
        {
            HashAlgorithm[] testAlgo = { HashAlgorithm.sha256, HashAlgorithm.sha384, HashAlgorithm.sha512 };
            foreach(HashAlgorithm ha in testAlgo)
            {
                using(RSA key = CreateKey())
                {
                    X509Certificate2 cert = CreateCertificate("CN=Test", key);
                    byte[] signedBytes = SignFixture("hello-world-unsigned.xlsx", key, new List<X509Certificate> { cert }, ha);

                    OPCPackage pkg = OPCPackage.Open(new MemoryStream(signedBytes));
                    try
                    {
                        ClassicAssert.IsTrue(NewSignatureInfo(pkg).VerifySignature(), "hash algorithm: " + ha.jceId);
                    }
                    finally
                    {
                        pkg.Revert();
                    }
                }
            }
        }

        [Test]
        public void TestMultiSign()
        {
            using(RSA key1 = CreateKey())
            using(RSA key2 = CreateKey())
            {
                X509Certificate2 cert1 = CreateCertificate("CN=Test1", key1);
                X509Certificate2 cert2 = CreateCertificate("CN=Test2", key2);
                byte[] signedOnce = SignFixture("hello-world-unsigned.docx", key1, new List<X509Certificate> { cert1 }, HashAlgorithm.sha1);
                byte[] signedTwice = Sign(new MemoryStream(signedOnce), key2, new List<X509Certificate> { cert2 }, HashAlgorithm.sha1);

                OPCPackage pkg = OPCPackage.Open(new MemoryStream(signedTwice));
                try
                {
                    List<X509Certificate> result = GetValidSigners(NewSignatureInfo(pkg));
                    ClassicAssert.AreEqual(2, result.Count);
                    ClassicAssert.AreEqual(cert1.Subject, result[0].Subject);
                    ClassicAssert.AreEqual(cert2.Subject, result[1].Subject);
                }
                finally
                {
                    pkg.Revert();
                }
            }
        }

        private static SignatureInfo NewSignatureInfo(OPCPackage pkg)
        {
            SignatureConfig sic = new SignatureConfig();
            sic.SetOpcPackage(pkg);
            SignatureInfo si = new SignatureInfo();
            si.SetSignatureConfig(sic);
            return si;
        }

        private static SignatureConfig NewSigningConfig(OPCPackage pkg, RSA key, List<X509Certificate> chain, HashAlgorithm digestAlgo)
        {
            SignatureConfig sic = new SignatureConfig();
            sic.SetOpcPackage(pkg);
            sic.SetKey(key);
            sic.SetSigningCertificateChain(chain);
            sic.SetExecutionTime(executionTime);
            sic.SetDigestAlgo(digestAlgo);
            return sic;
        }

        private static List<X509Certificate> GetValidSigners(SignatureInfo si)
        {
            List<X509Certificate> result = new List<X509Certificate>();
            foreach(SignatureInfo.SignaturePart sp in si.GetSignatureParts())
            {
                if(sp.Validate())
                {
                    result.Add(sp.GetSigner());
                }
            }
            return result;
        }

        private static byte[] SignFixture(String testFile, RSA key, List<X509Certificate> chain, HashAlgorithm digestAlgo)
        {
            using(Stream input = testdata.OpenResourceAsStream(testFile))
            {
                return Sign(input, key, chain, digestAlgo);
            }
        }

        private static byte[] Sign(Stream input, RSA key, List<X509Certificate> chain, HashAlgorithm digestAlgo)
        {
            OPCPackage pkg = OPCPackage.Open(input);
            try
            {
                SignatureInfo si = new SignatureInfo();
                si.SetSignatureConfig(NewSigningConfig(pkg, key, chain, digestAlgo));
                si.ConfirmSignature();

                MemoryStream bos = new MemoryStream();
                pkg.Save(bos);
                return bos.ToArray();
            }
            finally
            {
                pkg.Revert();
            }
        }

        private static RSA CreateKey()
        {
            return RSA.Create(2048);
        }

        private static X509Certificate2 CreateCertificate(String subjectDN, RSA key, bool isCA = false)
        {
            CertificateRequest req = new CertificateRequest(subjectDN, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            req.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyCertSign, false));
            if(isCA)
            {
                req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            }
            return req.CreateSelfSigned(new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero));
        }

        private static List<KeyValuePair<String, byte[]>> ReadZip(byte[] package)
        {
            List<KeyValuePair<String, byte[]>> entries = new List<KeyValuePair<String, byte[]>>();
            using(ZipInputStream zin = new ZipInputStream(new MemoryStream(package)))
            {
                ZipEntry entry;
                while((entry = zin.GetNextEntry()) != null)
                {
                    if(entry.IsDirectory)
                    {
                        continue;
                    }
                    MemoryStream bos = new MemoryStream();
                    zin.CopyTo(bos);
                    entries.Add(new KeyValuePair<String, byte[]>(entry.Name, bos.ToArray()));
                }
            }
            return entries;
        }

        private static XmlDocument LoadSignatureXml(byte[] package, String entryName)
        {
            foreach(KeyValuePair<String, byte[]> entry in ReadZip(package))
            {
                if(entry.Key == entryName)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(new MemoryStream(entry.Value));
                    return doc;
                }
            }
            throw new AssertionException("entry not found: " + entryName);
        }

        private static byte[] ReplaceInEntry(byte[] package, String entryName, String search, String replacement)
        {
            MemoryStream ms = new MemoryStream();
            bool replaced = false;
            using(ZipOutputStream zos = new ZipOutputStream(ms) { IsStreamOwner = false })
            {
                foreach(KeyValuePair<String, byte[]> entry in ReadZip(package))
                {
                    byte[] data = entry.Value;
                    if(entry.Key == entryName)
                    {
                        String content = Encoding.UTF8.GetString(data);
                        ClassicAssert.IsTrue(content.Contains(search), "entry " + entryName + " doesn't contain " + search);
                        data = Encoding.UTF8.GetBytes(content.Replace(search, replacement));
                        replaced = true;
                    }
                    zos.PutNextEntry(new ZipEntry(entry.Key));
                    zos.Write(data, 0, data.Length);
                    zos.CloseEntry();
                }
                zos.Finish();
            }
            ClassicAssert.IsTrue(replaced, "entry not found: " + entryName);
            return ms.ToArray();
        }
    }
}