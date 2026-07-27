# Cryptography

Provides thread-safe cryptographic helpers for AES/RSA encryption, hashing (HMAC), PBKDF2 key derivation, and X.509 certificate operations. Uses only System.Security.Cryptography types — no external NuGet packages.

## Classes

### CryptoHelper
Static class with self-contained cryptographic methods. All methods gracefully return null or safe defaults on failure, including on non-Windows platforms.

#### AES — Symmetric Encryption

| Method | Returns | Description |
|--------|---------|-------------|
| EncryptAes(byte[] data, byte[] key, byte[]? iv, AesMode, AesPadding) | byte[]? | Encrypts data using AES. Auto-generates random IV if none provided |
| DecryptAes(byte[] ciphertext, byte[] key, byte[]? iv, AesMode, AesPadding) | byte[]? | Decrypts AES ciphertext. Extracts prepended IV if iv is null |
| EncryptAesString(string plaintext, string password, int keySize) | byte[]? | Encrypts string with password-derived key (PBKDF2). Prepends 16-byte salt + IV |
| DecryptAesString(byte[] ciphertext, string password, int keySize) | string? | Decrypts string produced by EncryptAesString |

#### RSA — Asymmetric Encryption

| Method | Returns | Description |
|--------|---------|-------------|
| EncryptRsa(byte[] data, string publicKeyXml) | byte[]? | Encrypts data using RSA with OAEP-SHA256 padding |
| DecryptRsa(byte[] data, string privateKeyXml) | byte[]? | Decrypts data using RSA with OAEP-SHA256 padding |
| GenerateRsaKeyPair(int keySize) | (string PublicKey, string PrivateKey)? | Generates RSA key pair as XML strings |
| SignData(byte[] data, string privateKeyXml, HashAlgorithmName) | byte[]? | Creates RSA PKCS#1 v1.5 signature |
| VerifyData(byte[] data, byte[] signature, string publicKeyXml, HashAlgorithmName) | bool | Verifies RSA PKCS#1 v1.5 signature |

#### Hashing & Key Derivation

| Method | Returns | Description |
|--------|---------|-------------|
| ComputeHMACSHA256(string key, string data) | string | Computes HMAC-SHA256 as lowercase hex |
| ComputeHMACSHA512(string key, string data) | string | Computes HMAC-SHA512 as lowercase hex |
| ComputePBKDF2(string password, byte[] salt, int iterations, int outputLength) | string | Derives key using PBKDF2, returns hex |

#### Certificate Helpers

| Method | Returns | Description |
|--------|---------|-------------|
| LoadCertificateFromFile(string path, string? password) | X509Certificate2? | Loads certificate from .pfx or .cer file |
| LoadCertificateFromStore(string thumbprint, StoreName, StoreLocation) | X509Certificate2? | Loads certificate from store by thumbprint |
| CreateSelfSignedCertificate(string subjectName) | X509Certificate2? | Creates self-signed cert (.NET 6+ only) |
| GetCertificateThumbprint(X509Certificate2?) | string? | Gets thumbprint hex string |
| IsCertificateExpired(X509Certificate2?) | bool | Checks if certificate has expired |
| IsCertificateValidForSsl(X509Certificate2?) | bool | Checks for Server Authentication EKU |

### AesMode
Enum defining AES cipher modes (CBC, ECB, CFB, OFB, CTS).

### AesPadding
Enum defining AES padding schemes (None, PKCS7, Zeros, ANSIX923, ISO10126).

## Usage

```csharp
using BPlusLib.Foundation.Cryptography;

// AES encryption with password
byte[] encrypted = CryptoHelper.EncryptAesString("Secret message", "my-password");
string? decrypted = CryptoHelper.DecryptAesString(encrypted, "my-password");

// AES encryption with raw key
byte[] key = new byte[32]; // 256-bit key
byte[] iv = new byte[16];
byte[] ciphertext = CryptoHelper.EncryptAes(data, key, iv);
byte[] plaintext = CryptoHelper.DecryptAes(ciphertext, key, iv);

// RSA
var keys = CryptoHelper.GenerateRsaKeyPair(2048);
byte[] encrypted = CryptoHelper.EncryptRsa(data, keys.Value.PublicKey);
byte[] decrypted = CryptoHelper.DecryptRsa(encrypted, keys.Value.PrivateKey);

// HMAC
string hash = CryptoHelper.ComputeHMACSHA256("secret", "data");

// Certificate
var cert = CryptoHelper.LoadCertificateFromFile("cert.pfx", "password");
bool valid = CryptoHelper.IsCertificateValidForSsl(cert);
```

## Dependencies
- System.Security.Cryptography (Aes, RSA, HMACSHA256, HMACSHA512, Rfc2898DeriveBytes, RandomNumberGenerator)
- System.Security.Cryptography.X509Certificates (X509Certificate2, X509Store, CertificateRequest)
