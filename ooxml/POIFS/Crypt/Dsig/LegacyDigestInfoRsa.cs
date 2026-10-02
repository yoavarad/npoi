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
    using System;
    using System.Numerics;
    using System.Security.Cryptography;

    /**
     * RSA public key wrapper for verification only. It additionally accepts PKCS#1 v1.5
     * signatures whose DigestInfo AlgorithmIdentifier omits the NULL parameters.
     * Older POI / eID based signers produced this encoding (see SignatureConfig.GetHashMagic).
     * Windows CNG accepts it, but OpenSSL based platforms (Linux, macOS) don't, so the
     * check is done explicitly to get the same result on every platform.
     */
    internal sealed class LegacyDigestInfoRsa : RSA
    {
        private readonly RSA inner;

        internal LegacyDigestInfoRsa(RSA inner)
        {
            this.inner = inner;
            KeySizeValue = inner.KeySize;
            LegalKeySizesValue = new[] { new KeySizes(inner.KeySize, inner.KeySize, 0) };
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            if(includePrivateParameters)
            {
                throw new CryptographicException("verification only key");
            }
            return inner.ExportParameters(false);
        }

        public override void ImportParameters(RSAParameters parameters)
        {
            throw new NotSupportedException();
        }

        public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if(inner.VerifyHash(hash, signature, hashAlgorithm, padding))
            {
                return true;
            }
            if(padding != RSASignaturePadding.Pkcs1)
            {
                return false;
            }
            byte[] prefix = GetLegacyDigestInfoPrefix(hashAlgorithm);
            if(prefix == null || prefix[prefix.Length - 1] != hash.Length)
            {
                return false;
            }

            RSAParameters pub = inner.ExportParameters(false);
            int k = pub.Modulus.Length;
            if(signature.Length != k)
            {
                return false;
            }
            BigInteger n = ToBigInteger(pub.Modulus);
            BigInteger s = ToBigInteger(signature);
            if(s >= n)
            {
                return false;
            }
            byte[] em = ToBigEndian(BigInteger.ModPow(s, ToBigInteger(pub.Exponent), n), k);

            // EM = 0x00 || 0x01 || PS (0xFF..., >= 8 bytes) || 0x00 || DigestInfo
            int tLen = prefix.Length + hash.Length;
            int psLen = k - 3 - tLen;
            if(psLen < 8)
            {
                return false;
            }
            int diff = em[0] | (em[1] ^ 0x01) | em[2 + psLen];
            for(int i = 2; i < 2 + psLen; i++)
            {
                diff |= em[i] ^ 0xFF;
            }
            int t = 3 + psLen;
            for(int i = 0; i < prefix.Length; i++)
            {
                diff |= em[t + i] ^ prefix[i];
            }
            t += prefix.Length;
            for(int i = 0; i < hash.Length; i++)
            {
                diff |= em[t + i] ^ hash[i];
            }
            return diff == 0;
        }

        /**
         * DigestInfo prefixes with an AlgorithmIdentifier without NULL parameters.
         */
        private static byte[] GetLegacyDigestInfoPrefix(HashAlgorithmName hashAlgorithm)
        {
            if(hashAlgorithm == HashAlgorithmName.SHA1)
                return new byte[] { 0x30, 0x1f, 0x30, 0x07, 0x06, 0x05, 0x2b, 0x0e, 0x03, 0x02, 0x1a, 0x04, 0x14 };
            if(hashAlgorithm == HashAlgorithmName.SHA256)
                return new byte[] { 0x30, 0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20 };
            if(hashAlgorithm == HashAlgorithmName.SHA384)
                return new byte[] { 0x30, 0x3f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x02, 0x04, 0x30 };
            if(hashAlgorithm == HashAlgorithmName.SHA512)
                return new byte[] { 0x30, 0x4f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x03, 0x04, 0x40 };
            return null;
        }

        private static BigInteger ToBigInteger(byte[] bigEndian)
        {
            byte[] le = new byte[bigEndian.Length + 1];
            for(int i = 0; i < bigEndian.Length; i++)
            {
                le[i] = bigEndian[bigEndian.Length - 1 - i];
            }
            return new BigInteger(le);
        }

        private static byte[] ToBigEndian(BigInteger value, int length)
        {
            byte[] le = value.ToByteArray();
            byte[] be = new byte[length];
            for(int i = 0; i < le.Length && i < length; i++)
            {
                be[length - 1 - i] = le[i];
            }
            return be;
        }
    }
}