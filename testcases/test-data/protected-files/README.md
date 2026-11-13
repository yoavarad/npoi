# Password-protected / encrypted test files

All encrypted samples in one place. `POIDataSamples` falls back to this folder
(`../protected-files`) when a sample name is not found in its module folder, so tests
keep opening these by bare file name.

| File | Encryption | Password |
|---|---|---|
| password.xls | BIFF8 binary RC4 | `password` |
| 97_password.xls | BIFF8 RC4 | `Password1234_` |
| 97_rc4cryptoapi_password.xls | RC4 CryptoAPI | `Password1234_` |
| xor-encryption-abc.xls | XOR obfuscation | `abc` |
| xor-obfuscation-abc.doc | XOR obfuscation method 2 (plain: `document/xor-obfuscation-abc-plain.doc`) | `abc` |
| PasswordProtected.doc | Word encryption | unknown |
| Password_Protected-hello.ppt, -56-hello.ppt, -np-hello.ppt | PPT encryption | unknown |
| protect.xlsx | OOXML Standard | default (`Decryptor.DEFAULT_PASSWORD`) |
| protected_sha512.xlsx | OOXML Agile (SHA-512) | default (`Decryptor.DEFAULT_PASSWORD`) |
| protected_agile.docx | OOXML Agile | default (`Decryptor.DEFAULT_PASSWORD`) |
| bug53475-password-is-pass.docx | OOXML encrypted | `pass` |
| bug53475-password-is-solrcell.docx | OOXML encrypted | `solrcell` |
| extenxls_pwd123.xlsx | OOXML encrypted | `pwd123` |
| 2007_password.xlsx, 2007macro_password.xlsm | OOXML encrypted | `Password1234_` |
| 58616.xlsx | OOXML encrypted | see TestDecryptor |
| protected_passtika.xlsx | OOXML encrypted | `tika` |

Not moved: `*Protection*.xlsx/docx` (sheet/document protection flags, not encrypted).
