// <copyright file="CryptoHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace BPlusLib.Foundation.Cryptography
{
    /// <summary>
    /// Defines the cipher mode for AES symmetric encryption.
    /// Mapped directly to <see cref="CipherMode"/>.
    /// </summary>
    public enum AesMode
    {
        /// <summary>Cipher Block Chaining mode.</summary>
        CBC = 1,

        /// <summary>Electronic Codebook mode.</summary>
        ECB = 2,

        /// <summary>Cipher Feedback mode.</summary>
        CFB = 3,

        /// <summary>Output Feedback mode.</summary>
        OFB = 4,

        /// <summary>Cipher Text Stealing mode.</summary>
        CTS = 5,
    }

    /// <summary>
    /// Defines the padding scheme for AES symmetric encryption.
    /// Mapped directly to <see cref="PaddingMode"/>.
    /// </summary>
    public enum AesPadding
    {
        /// <summary>No padding.</summary>
        None = 0,

        /// <summary>PKCS#7 padding.</summary>
        PKCS7 = 1,

        /// <summary>Zero padding.</summary>
        Zeros = 2,

        /// <summary>ANSI X.923 padding.</summary>
        ANSIX923 = 3,

        /// <summary>ISO 10126 padding.</summary>
        ISO10126 = 4,
    }

    /// <summary>
    /// Provides thread-safe cryptographic helpers for AES, RSA, hashing,
    /// and X.509 certificate operations. All methods are self-contained
    /// (no shared state) and gracefully return <see langword="null"/> or
    /// safe defaults on failure, including on non-Windows platforms.
    /// </summary>
    /// <remarks>
    /// Uses only <see cref="System.Security.Cryptography"/> types — no
    /// external NuGet packages. Multi-targets net472, net6.0, and net8.0
    /// via conditional compilation directives.
    /// </remarks>
    public static class CryptoHelper
    {
        // -----------------------------------------------------------------
        // Constants
        // -----------------------------------------------------------------

        private const int DefaultPbkdf2Iterations = 100000;
        private const int SaltLength = 16;
        private const int IvLength = 16;

        // -----------------------------------------------------------------
        // AES — Symmetric Encryption
        // -----------------------------------------------------------------

        /// <summary>
        /// Encrypts <paramref name="data"/> using AES with the specified
        /// <paramref name="key"/>, <paramref name="iv"/>, mode, and padding.
        /// </summary>
        /// <param name="data">The plaintext bytes to encrypt.</param>
        /// <param name="key">The symmetric key (128, 192, or 256 bits).</param>
        /// <param name="iv">
        /// The initialization vector (16 bytes). If <see langword="null"/> or
        /// empty, a random IV is generated and prepended to the output.
        /// </param>
        /// <param name="mode">The cipher mode (default: <see cref="AesMode.CBC"/>).</param>
        /// <param name="padding">The padding scheme (default: <see cref="AesPadding.PKCS7"/>).</param>
        /// <returns>
        /// The ciphertext bytes with the random IV (16 bytes) prepended if
        /// <paramref name="iv"/> was <see langword="null"/> or empty;
        /// otherwise <see langword="null"/> on failure.
        /// </returns>
        public static byte[]? EncryptAes(
            byte[] data,
            byte[] key,
            byte[]? iv,
            AesMode mode = AesMode.CBC,
            AesPadding padding = AesPadding.PKCS7)
        {
            if (data is null || key is null)
                return null;

            try
            {
                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = MapMode(mode);
                aes.Padding = MapPadding(padding);

                bool prependIv = iv is null || iv.Length == 0;

                if (prependIv)
                {
                    aes.GenerateIV();
                    iv = aes.IV;
                }
                else
                {
                    aes.IV = iv!;
                }

                using var ms = new MemoryStream();

                // Prepend the IV when we generated it
                if (prependIv && iv is not null)
                {
                    ms.Write(iv, 0, iv.Length);
                }

                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                }

                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Decrypts <paramref name="ciphertext"/> using AES with the specified
        /// <paramref name="key"/>, mode, and padding.
        /// </summary>
        /// <param name="ciphertext">The ciphertext bytes to decrypt.</param>
        /// <param name="key">The symmetric key (128, 192, or 256 bits).</param>
        /// <param name="iv">
        /// The initialization vector (16 bytes). If <see langword="null"/> and
        /// <paramref name="ciphertext"/> is at least 16 bytes long, the first
        /// 16 bytes are treated as the prepended IV.
        /// </param>
        /// <param name="mode">The cipher mode (default: <see cref="AesMode.CBC"/>).</param>
        /// <param name="padding">The padding scheme (default: <see cref="AesPadding.PKCS7"/>).</param>
        /// <returns>
        /// The decrypted plaintext bytes, or <see langword="null"/> on failure.
        /// </returns>
        public static byte[]? DecryptAes(
            byte[] ciphertext,
            byte[] key,
            byte[]? iv = null,
            AesMode mode = AesMode.CBC,
            AesPadding padding = AesPadding.PKCS7)
        {
            if (ciphertext is null || key is null)
                return null;

            try
            {
                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = MapMode(mode);
                aes.Padding = MapPadding(padding);

                byte[] encryptedData;

                if (iv is null && ciphertext.Length >= IvLength)
                {
                    // Extract IV from the first 16 bytes
                    iv = new byte[IvLength];
                    Buffer.BlockCopy(ciphertext, 0, iv, 0, IvLength);
                    aes.IV = iv;

                    encryptedData = new byte[ciphertext.Length - IvLength];
                    Buffer.BlockCopy(ciphertext, IvLength, encryptedData, 0, encryptedData.Length);
                }
                else if (iv is not null)
                {
                    aes.IV = iv;
                    encryptedData = ciphertext;
                }
                else
                {
                    // No IV available
                    return null;
                }

                using var ms = new MemoryStream(encryptedData);
                using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var resultMs = new MemoryStream();

                cs.CopyTo(resultMs);
                return resultMs.ToArray();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Encrypts a <paramref name="plaintext"/> string using a
        /// password-derived key (PBKDF2-SHA1 with 100 000 iterations).
        /// </summary>
        /// <param name="plaintext">The plaintext string to encrypt.</param>
        /// <param name="password">The password used for key derivation.</param>
        /// <param name="keySize">The AES key size in bits (128, 192, or 256; default: 256).</param>
        /// <returns>
        /// The ciphertext with a 16-byte salt (for key derivation) and a
        /// 16-byte IV prepended, or <see langword="null"/> on failure.
        /// </returns>
        public static byte[]? EncryptAesString(
            string plaintext,
            string password,
            int keySize = 256)
        {
            if (plaintext is null || password is null)
                return null;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(plaintext);
                byte[] salt = new byte[SaltLength];

                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }

                byte[] key = DeriveKey(password, salt, keySize / 8);

                // Encrypt with a random IV (prepended)
                byte[]? ciphertext = EncryptAes(data, key, null);

                if (ciphertext is null)
                    return null;

                // Result: salt (16) + ciphertext (which already has IV prepended)
                byte[] result = new byte[SaltLength + ciphertext.Length];
                Buffer.BlockCopy(salt, 0, result, 0, SaltLength);
                Buffer.BlockCopy(ciphertext, 0, result, SaltLength, ciphertext.Length);

                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Decrypts a <paramref name="ciphertext"/> produced by
        /// <see cref="EncryptAesString"/>.
        /// </summary>
        /// <param name="ciphertext">
        /// The ciphertext with a 16-byte salt and 16-byte IV prepended.
        /// </param>
        /// <param name="password">The password used for key derivation.</param>
        /// <param name="keySize">The AES key size in bits (128, 192, or 256; default: 256).</param>
        /// <returns>
        /// The decrypted plaintext string, or <see langword="null"/> on failure.
        /// </returns>
        public static string? DecryptAesString(
            byte[] ciphertext,
            string password,
            int keySize = 256)
        {
            if (ciphertext is null || password is null)
                return null;

            if (ciphertext.Length <= SaltLength)
                return null;

            try
            {
                // Extract salt (first 16 bytes)
                byte[] salt = new byte[SaltLength];
                Buffer.BlockCopy(ciphertext, 0, salt, 0, SaltLength);

                byte[] key = DeriveKey(password, salt, keySize / 8);

                // The rest is the IV-prepended ciphertext (16 IV + data)
                byte[] encryptedData = new byte[ciphertext.Length - SaltLength];
                Buffer.BlockCopy(ciphertext, SaltLength, encryptedData, 0, encryptedData.Length);

                byte[]? decrypted = DecryptAes(encryptedData, key);

                if (decrypted is null)
                    return null;

                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return null;
            }
        }

        // -----------------------------------------------------------------
        // RSA — Asymmetric Encryption
        // -----------------------------------------------------------------

        /// <summary>
        /// Encrypts <paramref name="data"/> using RSA with OAEP-SHA256 padding.
        /// </summary>
        /// <param name="data">The plaintext bytes to encrypt.</param>
        /// <param name="publicKeyXml">The RSA public key in XML format.</param>
        /// <returns>The encrypted ciphertext bytes, or <see langword="null"/> on failure.</returns>
        public static byte[]? EncryptRsa(byte[] data, string publicKeyXml)
        {
            if (data is null || string.IsNullOrEmpty(publicKeyXml))
                return null;

            try
            {
                using var rsa = RSA.Create();
                LoadRsaFromXml(rsa, publicKeyXml);
                return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Decrypts <paramref name="data"/> using RSA with OAEP-SHA256 padding.
        /// </summary>
        /// <param name="data">The ciphertext bytes to decrypt.</param>
        /// <param name="privateKeyXml">The RSA private key in XML format.</param>
        /// <returns>The decrypted plaintext bytes, or <see langword="null"/> on failure.</returns>
        public static byte[]? DecryptRsa(byte[] data, string privateKeyXml)
        {
            if (data is null || string.IsNullOrEmpty(privateKeyXml))
                return null;

            try
            {
                using var rsa = RSA.Create();
                LoadRsaFromXml(rsa, privateKeyXml);
                return rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generates a new RSA key pair and returns it as XML strings.
        /// </summary>
        /// <param name="keySize">The key size in bits (default: 2048).</param>
        /// <returns>
        /// A tuple containing the public and private key XML strings,
        /// or <see langword="null"/> on failure.
        /// </returns>
        public static (string PublicKey, string PrivateKey)? GenerateRsaKeyPair(int keySize = 2048)
        {
            try
            {
                using var rsa = RSA.Create(keySize);
                string publicKey = SaveRsaToXml(rsa, false);
                string privateKey = SaveRsaToXml(rsa, true);
                return (publicKey, privateKey);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates an RSA PKCS#1 v1.5 signature over <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <param name="privateKeyXml">The RSA private key in XML format.</param>
        /// <param name="hashAlgorithm">
        /// The hash algorithm to use (defaults to SHA-256 when not specified).
        /// </param>
        /// <returns>The digital signature bytes, or <see langword="null"/> on failure.</returns>
        public static byte[]? SignData(
            byte[] data,
            string privateKeyXml,
            HashAlgorithmName hashAlgorithm = default)
        {
            if (data is null || string.IsNullOrEmpty(privateKeyXml))
                return null;

            HashAlgorithmName hash = ResolveHashAlgorithm(hashAlgorithm);

            try
            {
                using var rsa = RSA.Create();
                LoadRsaFromXml(rsa, privateKeyXml);
                return rsa.SignData(data, hash, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Verifies an RSA PKCS#1 v1.5 signature over <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The data that was signed.</param>
        /// <param name="signature">The digital signature to verify.</param>
        /// <param name="publicKeyXml">The RSA public key in XML format.</param>
        /// <param name="hashAlgorithm">
        /// The hash algorithm used to sign (defaults to SHA-256 when not specified).
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the signature is valid; otherwise <see langword="false"/>.
        /// </returns>
        public static bool VerifyData(
            byte[] data,
            byte[] signature,
            string publicKeyXml,
            HashAlgorithmName hashAlgorithm = default)
        {
            if (data is null || signature is null || string.IsNullOrEmpty(publicKeyXml))
                return false;

            HashAlgorithmName hash = ResolveHashAlgorithm(hashAlgorithm);

            try
            {
                using var rsa = RSA.Create();
                LoadRsaFromXml(rsa, publicKeyXml);
                return rsa.VerifyData(data, signature, hash, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------------------------------------------
        // Hashing — HMAC & Key Derivation
        // -----------------------------------------------------------------

        /// <summary>
        /// Computes the HMAC-SHA256 of <paramref name="data"/> using <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The HMAC secret key.</param>
        /// <param name="data">The data to authenticate.</param>
        /// <returns>
        /// A lowercase hex-encoded HMAC string, or <see cref="string.Empty"/> on failure.
        /// </returns>
        public static string ComputeHMACSHA256(string key, string data)
        {
            if (key is null || data is null)
                return string.Empty;

            try
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);

                using var hmac = new HMACSHA256(keyBytes);
                byte[] hash = hmac.ComputeHash(dataBytes);
                return BytesToHex(hash);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Computes the HMAC-SHA512 of <paramref name="data"/> using <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The HMAC secret key.</param>
        /// <param name="data">The data to authenticate.</param>
        /// <returns>
        /// A lowercase hex-encoded HMAC string, or <see cref="string.Empty"/> on failure.
        /// </returns>
        public static string ComputeHMACSHA512(string key, string data)
        {
            if (key is null || data is null)
                return string.Empty;

            try
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);

                using var hmac = new HMACSHA512(keyBytes);
                byte[] hash = hmac.ComputeHash(dataBytes);
                return BytesToHex(hash);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Derives a key from <paramref name="password"/> using PBKDF2.
        /// </summary>
        /// <param name="password">The password to derive a key from.</param>
        /// <param name="salt">The cryptographic salt.</param>
        /// <param name="iterations">The number of iterations (default: 100 000).</param>
        /// <param name="outputLength">The output length in bytes (default: 32).</param>
        /// <returns>
        /// A lowercase hex-encoded derived key string, or <see cref="string.Empty"/> on failure.
        /// </returns>
        public static string ComputePBKDF2(
            string password,
            byte[] salt,
            int iterations = DefaultPbkdf2Iterations,
            int outputLength = 32)
        {
            if (password is null || salt is null || salt.Length == 0)
                return string.Empty;

            try
            {
                byte[] derived = DeriveKey(password, salt, outputLength, iterations);
                return BytesToHex(derived);
            }
            catch
            {
                return string.Empty;
            }
        }

        // -----------------------------------------------------------------
        // Certificate Helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Loads an X.509 certificate from a file (.pfx or .cer).
        /// </summary>
        /// <param name="path">The file path to the certificate.</param>
        /// <param name="password">
        /// The optional password for PFX files. May be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// The loaded <see cref="X509Certificate2"/>, or <see langword="null"/> on failure.
        /// </returns>
        public static X509Certificate2? LoadCertificateFromFile(string path, string? password = null)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            try
            {
                if (password is not null)
                {
                    return new X509Certificate2(path, password);
                }

                return new X509Certificate2(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Loads an X.509 certificate from the specified certificate store
        /// by thumbprint.
        /// </summary>
        /// <param name="thumbprint">The certificate thumbprint (hex string).</param>
        /// <param name="storeName">The store name (default: <see cref="StoreName.My"/>).</param>
        /// <param name="location">The store location (default: <see cref="StoreLocation.CurrentUser"/>).</param>
        /// <returns>
        /// The matching <see cref="X509Certificate2"/>, or <see langword="null"/>
        /// if not found or on failure.
        /// </returns>
        public static X509Certificate2? LoadCertificateFromStore(
            string thumbprint,
            StoreName storeName = StoreName.My,
            StoreLocation location = StoreLocation.CurrentUser)
        {
            if (string.IsNullOrEmpty(thumbprint))
                return null;

            try
            {
                using var store = new X509Store(storeName, location);
                store.Open(OpenFlags.ReadOnly);

                var certificates = store.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    thumbprint,
                    false);

                return certificates.Count > 0 ? certificates[0] : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a self-signed X.509 certificate for testing purposes.
        /// </summary>
        /// <param name="subjectName">
        /// The subject distinguished name (e.g., <c>"CN=Test"</c>).
        /// </param>
        /// <returns>
        /// A self-signed <see cref="X509Certificate2"/> valid for 10 years,
        /// or <see langword="null"/> if the platform does not support
        /// programmatic certificate creation (net472 fallback).
        /// </returns>
        /// <remarks>
        /// On .NET Framework 4.7.2 this method returns <see langword="null"/>
        /// because the modern <see cref="CertificateRequest"/> API is not
        /// available without external dependencies (BouncyCastle).
        /// On .NET 6+ a self-signed certificate with Server Authentication
        /// EKU, digital signature, and key encipherment usages is created.
        /// </remarks>
        public static X509Certificate2? CreateSelfSignedCertificate(string subjectName)
        {
            if (string.IsNullOrEmpty(subjectName))
                return null;

            try
            {
#if NET472
                // CertificateRequest is not available on net472 without
                // BouncyCastle. Return null as per design.
                throw new PlatformNotSupportedException(
                    "Self-signed certificate creation requires .NET 6+");
#else
                using var rsa = RSA.Create(2048);

                var subject = new X500DistinguishedName(subjectName);

                var request = new CertificateRequest(
                    subject,
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // Basic constraints: not a CA
                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(
                        certificateAuthority: false,
                        hasPathLengthConstraint: false,
                        pathLengthConstraint: 0,
                        critical: false));

                // Key usage: digital signature + key encipherment
                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                        critical: false));

                // Enhanced key usage: Server Authentication
                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection
                        {
                            new Oid("1.3.6.1.5.5.7.3.1"), // Server Authentication
                        },
                        critical: false));

                // Create self-signed (valid from yesterday to +10 years)
                var cert = request.CreateSelfSigned(
                    DateTimeOffset.Now.AddDays(-1),
                    DateTimeOffset.Now.AddYears(10));

                return cert;
#endif
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the thumbprint of an X.509 certificate as a hex string.
        /// </summary>
        /// <param name="cert">The certificate.</param>
        /// <returns>
        /// The thumbprint string, or <see langword="null"/> if <paramref name="cert"/>
        /// is <see langword="null"/>.
        /// </returns>
        public static string? GetCertificateThumbprint(X509Certificate2? cert)
        {
            return cert?.Thumbprint;
        }

        /// <summary>
        /// Checks whether the X.509 certificate has expired.
        /// </summary>
        /// <param name="cert">The certificate.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="cert"/> is expired
        /// or <see langword="null"/>; otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsCertificateExpired(X509Certificate2? cert)
        {
            if (cert is null)
                return true;

            try
            {
                return DateTime.UtcNow > cert.NotAfter.ToUniversalTime();
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Checks whether the X.509 certificate is valid for SSL/TLS
        /// Server Authentication (EKU OID 1.3.6.1.5.5.7.3.1).
        /// </summary>
        /// <param name="cert">The certificate.</param>
        /// <returns>
        /// <see langword="true"/> if the certificate has the Server
        /// Authentication EKU; <see langword="false"/> otherwise or
        /// on failure.
        /// </returns>
        public static bool IsCertificateValidForSsl(X509Certificate2? cert)
        {
            if (cert is null)
                return false;

            try
            {
                foreach (var extension in cert.Extensions)
                {
                    if (extension is X509EnhancedKeyUsageExtension eku)
                    {
                        foreach (var oid in eku.EnhancedKeyUsages)
                        {
                            if (string.Equals(
                                    oid.Value,
                                    "1.3.6.1.5.5.7.3.1",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------------------------------------------
        // Private Helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Maps <see cref="AesMode"/> to <see cref="CipherMode"/>.
        /// </summary>
        private static CipherMode MapMode(AesMode mode)
        {
            return mode switch
            {
                AesMode.CBC => CipherMode.CBC,
                AesMode.ECB => CipherMode.ECB,
                AesMode.CFB => CipherMode.CFB,
                AesMode.OFB => CipherMode.OFB,
                AesMode.CTS => CipherMode.CTS,
                _ => CipherMode.CBC,
            };
        }

        /// <summary>
        /// Maps <see cref="AesPadding"/> to <see cref="PaddingMode"/>.
        /// </summary>
        private static PaddingMode MapPadding(AesPadding padding)
        {
            return padding switch
            {
                AesPadding.None => PaddingMode.None,
                AesPadding.PKCS7 => PaddingMode.PKCS7,
                AesPadding.Zeros => PaddingMode.Zeros,
                AesPadding.ANSIX923 => PaddingMode.ANSIX923,
                AesPadding.ISO10126 => PaddingMode.ISO10126,
                _ => PaddingMode.PKCS7,
            };
        }

        /// <summary>
        /// Resolves a <see cref="HashAlgorithmName"/> to SHA-256 when the
        /// default value (null name) is passed.
        /// </summary>
        private static HashAlgorithmName ResolveHashAlgorithm(HashAlgorithmName name)
        {
            return name.Name is null ? HashAlgorithmName.SHA256 : name;
        }

        /// <summary>
        /// Loads an RSA key from XML format. Handles cross-framework
        /// differences (<c>FromXmlString</c> is obsolete on .NET 6+).
        /// </summary>
        private static void LoadRsaFromXml(RSA rsa, string xmlKey)
        {
#if NET472
            rsa.FromXmlString(xmlKey);
#else
#pragma warning disable SYSLIB0043 // FromXmlString/ToXmlString are obsolete
            rsa.FromXmlString(xmlKey);
#pragma warning restore SYSLIB0043
#endif
        }

        /// <summary>
        /// Saves an RSA key to XML format. Handles cross-framework
        /// differences (<c>ToXmlString</c> is obsolete on .NET 6+).
        /// </summary>
        private static string SaveRsaToXml(RSA rsa, bool includePrivateParameters)
        {
#if NET472
            return rsa.ToXmlString(includePrivateParameters);
#else
#pragma warning disable SYSLIB0043 // FromXmlString/ToXmlString are obsolete
            return rsa.ToXmlString(includePrivateParameters);
#pragma warning restore SYSLIB0043
#endif
        }

        /// <summary>
        /// Derives a key from <paramref name="password"/> using PBKDF2.
        /// Uses SHA-1 on net472 (default for <see cref="Rfc2898DeriveBytes"/>)
        /// and SHA-256 on .NET 6+.
        /// </summary>
        private static byte[] DeriveKey(
            string password,
            byte[] salt,
            int outputLength,
            int iterations = DefaultPbkdf2Iterations)
        {
#if NET472
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations);
#else
            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256);
#endif
            return deriveBytes.GetBytes(outputLength);
        }

        /// <summary>
        /// Converts a byte array to a lowercase hex string.
        /// Uses platform-optimised path on .NET 6+ and a StringBuilder
        /// fallback on .NET Framework 4.7.2.
        /// </summary>
        private static string BytesToHex(byte[] bytes)
        {
            if (bytes is null || bytes.Length == 0)
                return string.Empty;

#if NET472
            var sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
#else
            return Convert.ToHexString(bytes).ToLowerInvariant();
#endif
        }
    }
}
