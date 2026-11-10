# XOR obfuscation (Method 2) .doc fixtures

Used by `testcases/main/POIFS/Crypt/TestXorObfuscationMethod2.cs`.

| File | Size | MD5 |
| --- | --- | --- |
| `xor-obfuscation-abc-plain.doc` | 27136 B | `2bcbff47456e5843dc4fbcddf6fc9ee2` |
| `xor-obfuscation-abc.doc` | 27136 B | `ca3edb5a68327cb5faea2d7f3d6f8b6e` |

## Provenance

1. `xor-obfuscation-abc-plain.doc` is Apache POI `test-data/document/password_password_cryptoapi.doc`
   (Apache-2.0) decrypted with msoffcrypto-tool, password `password`.
2. `xor-obfuscation-abc.doc` is the plain file obfuscated with XOR Method 2
   ([MS-OFFCRYPTO] 2.3.7.4-2.3.7.6, [MS-DOC] 2.2.6.1), password `abc`, by the script below
   (`python make_xor_doc.py xor-obfuscation-abc-plain.doc xor-obfuscation-abc.doc abc`).
   Microsoft Word (Office 16) opened it with password `abc` and showed `This is a test`.

No public XOR-obfuscated `.doc` was found, and Word 2016+ cannot save XOR, so the fixture is generated.

## Generator (Python 3 + olefile)

```python
# usage: make_xor_doc.py <plain.doc> <out.doc> <password>
import sys, shutil, struct, olefile
INITIAL_CODE = [0xE1F0,0x1D0F,0xCC9C,0x84C0,0x110C,0x0E10,0xF1CE,0x313E,0x1872,0xE139,0xD40F,0x84F9,0x280C,0xA96A,0x4EC3]
XOR_MATRIX = [0xAEFC,0x4DD9,0x9BB2,0x2745,0x4E8A,0x9D14,0x2A09,0x7B61,0xF6C2,0xFDA5,0xEB6B,0xC6F7,0x9DCF,0x2BBF,0x4563,0x8AC6,0x05AD,0x0B5A,0x16B4,0x2D68,0x5AD0,0x0375,0x06EA,0x0DD4,0x1BA8,0x3750,0x6EA0,0xDD40,0xD849,0xA0B3,0x5147,0xA28E,0x553D,0xAA7A,0x44D5,0x6F45,0xDE8A,0xAD35,0x4A4B,0x9496,0x390D,0x721A,0xEB23,0xC667,0x9CEF,0x29FF,0x53FE,0xA7FC,0x5FD9,0x47D3,0x8FA6,0x0F6D,0x1EDA,0x3DB4,0x7B68,0xF6D0,0xB861,0x60E3,0xC1C6,0x93AD,0x377B,0x6EF6,0xDDEC,0x45A0,0x8B40,0x06A1,0x0D42,0x1A84,0x3508,0x6A10,0xAA51,0x4483,0x8906,0x022D,0x045A,0x08B4,0x1168,0x76B4,0xED68,0xCAF1,0x85C3,0x1BA7,0x374E,0x6E9C,0x3730,0x6E60,0xDCC0,0xA9A1,0x4363,0x86C6,0x1DAD,0x3331,0x6662,0xCCC4,0x89A9,0x0373,0x06E6,0x0DCC,0x1021,0x2042,0x4084,0x8108,0x1231,0x2462,0x48C4]
PAD = [0xBB,0xFF,0xFF,0xBA,0xFF,0xFF,0xB9,0x80,0x00,0xBE,0x0F,0x00,0xBF,0x0F,0x00]
def ror(b): return ((b >> 1) | (b << 7)) & 0xFF
def single_byte(pw): return bytes([(ord(c) & 0xFF) or ((ord(c) >> 8) & 0xFF) for c in pw])[:15]
def xor_key_m1(p):
    k = INITIAL_CODE[len(p) - 1]; cur = 0x68
    for ch in reversed(p):
        for _ in range(7):
            if ch & 0x40: k ^= XOR_MATRIX[cur]
            ch = (ch * 2) & 0xFF; cur -= 1
    return k
def verifier_m1(p):
    v = 0
    for b in reversed(bytes([len(p)]) + p):
        v = ((1 if v & 0x4000 else 0) | ((v * 2) & 0x7FFF)) ^ b
    return v ^ 0xCE4B
def verifier_m2(p): return (xor_key_m1(p) << 16) | verifier_m1(p)
def xor_array_m2(p):
    hw = verifier_m2(p) >> 16; kh, kl = hw >> 8, hw & 0xFF
    a = list(p) + PAD[:16 - len(p)]
    for i in range(0, 16, 2):
        a[i] = ror(a[i] ^ kl); a[i + 1] = ror(a[i + 1] ^ kh)
    return a
def transform_m2(data, arr, start=0):
    out = bytearray(data)
    for off in range(start, len(out)):
        b = out[off]; x = b ^ arr[off & 15]
        if b != 0 and x != 0: out[off] = x
    return bytes(out)
src, dst, pw = sys.argv[1:4]; p = single_byte(pw); arr = xor_array_m2(p)
shutil.copyfile(src, dst); ole = olefile.OleFileIO(dst, write_mode=True)
wd = bytearray(ole.openstream('WordDocument').read()); flags = struct.unpack_from('<H', wd, 0x0A)[0]
tbl = '1Table' if flags & 0x0200 else '0Table'
wd = bytearray(transform_m2(bytes(wd), arr, 68))
struct.pack_into('<H', wd, 0x0A, flags | 0x0100 | 0x8000); struct.pack_into('<I', wd, 0x0E, verifier_m2(p))
ole.write_stream('WordDocument', bytes(wd)); ole.write_stream(tbl, transform_m2(ole.openstream(tbl).read(), arr))
if ole.exists('Data'): ole.write_stream('Data', transform_m2(ole.openstream('Data').read(), arr))
ole.close()
```
