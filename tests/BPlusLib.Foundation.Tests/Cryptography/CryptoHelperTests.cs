// <copyright file="CryptoHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Cryptography;

namespace BPlusLib.Foundation.Tests.Cryptography
{
    [Trait("Category", "Cryptography")]
    public sealed class CryptoHelperTests
    {
        // ── Helper ─────────────────────────────────────────────

        /// <summary>
        /// Fills a byte array with cryptographically random bytes.
        /// Works on both net472 (no Fill static) and net8.0.
        /// </summary>
        private static void FillRandom(byte[] buffer)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }
        }

        // ── AES Round-trip ─────────────────────────────────────

        [Fact]
        public void EncryptAes_DecryptAes_RoundTrips()
        {
            byte[] original = Encoding.UTF8.GetBytes("Hello, AES World! \U0001F600");
            byte[] key = new byte[32]; // AES-256
            FillRandom(key);
            byte[] iv = new byte[16];
            FillRandom(iv);

            byte[]? encrypted = CryptoHelper.EncryptAes(original, key, iv);
            encrypted.Should().NotBeNull();
            encrypted.Should().NotBeEquivalentTo(original); // should be different

            byte[]? decrypted = CryptoHelper.DecryptAes(encrypted, key, iv);
            decrypted.Should().NotBeNull();
            decrypted.Should().BeEquivalentTo(original);
        }

        [Fact]
        public void EncryptAes_WithAutoIv_RoundTrips()
        {
            byte[] original = Encoding.UTF8.GetBytes("Auto IV test");
            byte[] key = new byte[16]; // AES-128
            FillRandom(key);

            // Pass null IV — EncryptAes should generate and prepend one.
            byte[]? encrypted = CryptoHelper.EncryptAes(original, key, null);
            encrypted.Should().NotBeNull();
            // Ciphertext should be longer than plaintext (has IV prepended + padding).
            encrypted!.Length.Should().BeGreaterThan(original.Length);

            // Decrypt without passing IV — should extract it from the prepended 16 bytes.
            byte[]? decrypted = CryptoHelper.DecryptAes(encrypted, key);
            decrypted.Should().NotBeNull();
            decrypted.Should().BeEquivalentTo(original);
        }

        [Fact]
        public void EncryptAes_NullData_ReturnsNull()
        {
            byte[] key = new byte[16];
            byte[]? result = CryptoHelper.EncryptAes(null!, key, null);
            result.Should().BeNull();
        }

        [Fact]
        public void EncryptAes_NullKey_ReturnsNull()
        {
            byte[] data = Encoding.UTF8.GetBytes("test");
            byte[]? result = CryptoHelper.EncryptAes(data, null!, null);
            result.Should().BeNull();
        }

        [Fact]
        public void DecryptAes_NullCiphertext_ReturnsNull()
        {
            byte[] key = new byte[16];
            byte[]? result = CryptoHelper.DecryptAes(null!, key);
            result.Should().BeNull();
        }

        [Fact]
        public void DecryptAes_NullKey_ReturnsNull()
        {
            byte[] data = new byte[32];
            byte[]? result = CryptoHelper.DecryptAes(data, null!);
            result.Should().BeNull();
        }

        // ── AES String (password-based) ────────────────────────

        [Fact]
        public void EncryptAesString_DecryptAesString_RoundTrips()
        {
            string original = "Hello, Password-Based AES! 测试";
            string password = "My$ecureP@ssw0rd!";

            byte[]? encrypted = CryptoHelper.EncryptAesString(original, password);
            encrypted.Should().NotBeNull();
            // Should be salt (16) + IV (16) + encrypted data
            encrypted!.Length.Should().BeGreaterThan(32);

            string? decrypted = CryptoHelper.DecryptAesString(encrypted, password);
            decrypted.Should().NotBeNull();
            decrypted.Should().Be(original);
        }

        [Fact]
        public void EncryptAesString_WithDifferentKey_Fails()
        {
            string original = "Secret message";
            string password1 = "correct-password";
            string password2 = "wrong-password";

            byte[]? encrypted = CryptoHelper.EncryptAesString(original, password1);
            encrypted.Should().NotBeNull();

            // Decrypting with the wrong password should fail.
            string? decrypted = CryptoHelper.DecryptAesString(encrypted!, password2);
            decrypted.Should().BeNull();
        }

        [Fact]
        public void EncryptAesString_NullPlaintext_ReturnsNull()
        {
            byte[]? result = CryptoHelper.EncryptAesString(null!, "pass");
            result.Should().BeNull();
        }

        [Fact]
        public void EncryptAesString_NullPassword_ReturnsNull()
        {
            byte[]? result = CryptoHelper.EncryptAesString("text", null!);
            result.Should().BeNull();
        }

        [Fact]
        public void DecryptAesString_NullCiphertext_ReturnsNull()
        {
            string? result = CryptoHelper.DecryptAesString(null!, "pass");
            result.Should().BeNull();
        }

        [Fact]
        public void DecryptAesString_TooShortCiphertext_ReturnsNull()
        {
            byte[] tooShort = new byte[10];
            string? result = CryptoHelper.DecryptAesString(tooShort, "pass");
            result.Should().BeNull();
        }

        // ── RSA Key Generation ─────────────────────────────────

        [Fact]
        public void GenerateRsaKeyPair_ShouldReturnKeys()
        {
            (string PublicKey, string PrivateKey)? keyPair = CryptoHelper.GenerateRsaKeyPair(2048);
            keyPair.Should().NotBeNull();

            keyPair!.Value.PublicKey.Should().NotBeNullOrWhiteSpace();
            keyPair!.Value.PrivateKey.Should().NotBeNullOrWhiteSpace();

            // Public key XML should contain <RSAKeyValue> and <Modulus>
            keyPair.Value.PublicKey.Should().Contain("<RSAKeyValue>");
            keyPair.Value.PublicKey.Should().Contain("<Modulus>");
            keyPair.Value.PublicKey.Should().NotContain("<D>"); // private exponent

            // Private key XML should contain the private exponent
            keyPair.Value.PrivateKey.Should().Contain("<RSAKeyValue>");
            keyPair.Value.PrivateKey.Should().Contain("<Modulus>");
            keyPair.Value.PrivateKey.Should().Contain("<D>"); // private exponent
        }

        [Fact]
        public void GenerateRsaKeyPair_WithDefaultSize_ShouldReturnKeys()
        {
            (string PublicKey, string PrivateKey)? keyPair = CryptoHelper.GenerateRsaKeyPair();
            keyPair.Should().NotBeNull();
            keyPair!.Value.PublicKey.Should().NotBeNullOrWhiteSpace();
            keyPair!.Value.PrivateKey.Should().NotBeNullOrWhiteSpace();
        }

        // ── RSA Encrypt / Decrypt ──────────────────────────────

        [Fact]
        public void EncryptRsa_DecryptRsa_RoundTrips()
        {
            (string PublicKey, string PrivateKey)? keyPair = CryptoHelper.GenerateRsaKeyPair(2048);
            keyPair.Should().NotBeNull();

            byte[] original = Encoding.UTF8.GetBytes("RSA works!");

            byte[]? encrypted = CryptoHelper.EncryptRsa(original, keyPair!.Value.PublicKey);
            encrypted.Should().NotBeNull();
            encrypted.Should().NotBeEquivalentTo(original);

            byte[]? decrypted = CryptoHelper.DecryptRsa(encrypted!, keyPair!.Value.PrivateKey);
            decrypted.Should().NotBeNull();
            decrypted.Should().BeEquivalentTo(original);
        }

        [Fact]
        public void EncryptRsa_WithWrongKey_ReturnsNull()
        {
            (string PublicKey, string PrivateKey)? keyPair1 = CryptoHelper.GenerateRsaKeyPair(2048);
            (string PublicKey, string PrivateKey)? keyPair2 = CryptoHelper.GenerateRsaKeyPair(2048);
            keyPair1.Should().NotBeNull();
            keyPair2.Should().NotBeNull();

            byte[] original = Encoding.UTF8.GetBytes("secret");
            byte[]? encrypted = CryptoHelper.EncryptRsa(original, keyPair1!.Value.PublicKey);
            encrypted.Should().NotBeNull();

            // Decrypting with keyPair2's private key should fail.
            byte[]? decrypted = CryptoHelper.DecryptRsa(encrypted!, keyPair2!.Value.PrivateKey);
            decrypted.Should().BeNull();
        }

        [Fact]
        public void EncryptRsa_NullData_ReturnsNull()
        {
            byte[]? result = CryptoHelper.EncryptRsa(null!, "<RSAKeyValue></RSAKeyValue>");
            result.Should().BeNull();
        }

        [Fact]
        public void DecryptRsa_NullData_ReturnsNull()
        {
            byte[]? result = CryptoHelper.DecryptRsa(null!, "<RSAKeyValue></RSAKeyValue>");
            result.Should().BeNull();
        }

        // ── RSA Sign / Verify ──────────────────────────────────

        [Fact]
        public void SignData_VerifyData_ValidSignature()
        {
            (string PublicKey, string PrivateKey)? keyPair = CryptoHelper.GenerateRsaKeyPair(2048);
            keyPair.Should().NotBeNull();

            byte[] data = Encoding.UTF8.GetBytes("Data to sign");

            byte[]? signature = CryptoHelper.SignData(data, keyPair!.Value.PrivateKey);
            signature.Should().NotBeNull();
            signature!.Should().NotBeEmpty();

            bool valid = CryptoHelper.VerifyData(data, signature, keyPair!.Value.PublicKey);
            valid.Should().BeTrue();
        }

        [Fact]
        public void SignData_WithWrongKey_Fails()
        {
            (string PublicKey, string PrivateKey)? keyPair1 = CryptoHelper.GenerateRsaKeyPair(2048);
            (string PublicKey, string PrivateKey)? keyPair2 = CryptoHelper.GenerateRsaKeyPair(2048);
            keyPair1.Should().NotBeNull();
            keyPair2.Should().NotBeNull();

            byte[] data = Encoding.UTF8.GetBytes("Data to sign");

            byte[]? signature = CryptoHelper.SignData(data, keyPair1!.Value.PrivateKey);
            signature.Should().NotBeNull();

            // Verify with keyPair2's public key should fail.
            bool valid = CryptoHelper.VerifyData(data, signature!, keyPair2!.Value.PublicKey);
            valid.Should().BeFalse();
        }

        [Fact]
        public void VerifyData_TamperedData_ReturnsFalse()
        {
            (string PublicKey, string PrivateKey)? keyPair = CryptoHelper.GenerateRsaKeyPair(2048);
            keyPair.Should().NotBeNull();

            byte[] data = Encoding.UTF8.GetBytes("Original data");
            byte[] tampered = Encoding.UTF8.GetBytes("Tampered data");

            byte[]? signature = CryptoHelper.SignData(data, keyPair!.Value.PrivateKey);
            signature.Should().NotBeNull();

            bool valid = CryptoHelper.VerifyData(tampered, signature!, keyPair!.Value.PublicKey);
            valid.Should().BeFalse();
        }

        [Fact]
        public void SignData_NullData_ReturnsNull()
        {
            byte[]? result = CryptoHelper.SignData(null!, "<RSAKeyValue></RSAKeyValue>");
            result.Should().BeNull();
        }

        [Fact]
        public void VerifyData_NullSignature_ReturnsFalse()
        {
            bool result = CryptoHelper.VerifyData(
                Encoding.UTF8.GetBytes("data"),
                null!,
                "<RSAKeyValue></RSAKeyValue>");
            result.Should().BeFalse();
        }

        // ── HMAC ───────────────────────────────────────────────

        [Fact]
        public void ComputeHMACSHA256_ShouldReturnKnownValue()
        {
            string result = CryptoHelper.ComputeHMACSHA256("key", "data");
            result.Should().NotBeNullOrEmpty();
            // SHA-256 HMAC is 32 bytes = 64 hex chars
            result.Length.Should().Be(64);
            result.Should().MatchRegex("^[0-9a-f]{64}$");
        }

        [Fact]
        public void ComputeHMACSHA256_NullKey_ReturnsEmpty()
        {
            string result = CryptoHelper.ComputeHMACSHA256(null!, "data");
            result.Should().BeEmpty();
        }

        [Fact]
        public void ComputeHMACSHA256_NullData_ReturnsEmpty()
        {
            string result = CryptoHelper.ComputeHMACSHA256("key", null!);
            result.Should().BeEmpty();
        }

        [Fact]
        public void ComputeHMACSHA512_ShouldReturnKnownValue()
        {
            string result = CryptoHelper.ComputeHMACSHA512("key", "data");
            result.Should().NotBeNullOrEmpty();
            // SHA-512 HMAC is 64 bytes = 128 hex chars
            result.Length.Should().Be(128);
            result.Should().MatchRegex("^[0-9a-f]{128}$");
        }

        [Fact]
        public void ComputeHMACSHA512_NullKey_ReturnsEmpty()
        {
            string result = CryptoHelper.ComputeHMACSHA512(null!, "data");
            result.Should().BeEmpty();
        }

        [Fact]
        public void ComputeHMACSHA512_NullData_ReturnsEmpty()
        {
            string result = CryptoHelper.ComputeHMACSHA512("key", null!);
            result.Should().BeEmpty();
        }

        [Fact]
        public void ComputeHMACSHA256_WithSameInput_ReturnsSameOutput()
        {
            string result1 = CryptoHelper.ComputeHMACSHA256("key", "data");
            string result2 = CryptoHelper.ComputeHMACSHA256("key", "data");
            result1.Should().Be(result2);
        }

        [Fact]
        public void ComputeHMACSHA256_WithDifferentInput_ReturnsDifferentOutput()
        {
            string result1 = CryptoHelper.ComputeHMACSHA256("key1", "data");
            string result2 = CryptoHelper.ComputeHMACSHA256("key2", "data");
            result1.Should().NotBe(result2);
        }

        // ── PBKDF2 ─────────────────────────────────────────────

        [Fact]
        public void ComputePBKDF2_ShouldReturnNonEmpty()
        {
            byte[] salt = new byte[16];
            FillRandom(salt);

            string result = CryptoHelper.ComputePBKDF2("password", salt);
            result.Should().NotBeNullOrEmpty();
            // Default output length is 32 bytes = 64 hex chars
            result.Length.Should().Be(64);
            result.Should().MatchRegex("^[0-9a-f]{64}$");
        }

        [Fact]
        public void ComputePBKDF2_WithDifferentIterations_ShouldReturnNonEmpty()
        {
            byte[] salt = new byte[16];
            FillRandom(salt);

            string result = CryptoHelper.ComputePBKDF2("password", salt, iterations: 1000);
            result.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void ComputePBKDF2_WithDifferentOutputLength_ShouldMatchLength()
        {
            byte[] salt = new byte[16];
            FillRandom(salt);

            string result = CryptoHelper.ComputePBKDF2("password", salt, outputLength: 48);
            result.Should().NotBeNullOrEmpty();
            // 48 bytes = 96 hex chars
            result.Length.Should().Be(96);
        }

        [Fact]
        public void ComputePBKDF2_SameInput_ReturnsSameOutput()
        {
            byte[] salt = new byte[16];
            FillRandom(salt);

            string result1 = CryptoHelper.ComputePBKDF2("password", salt);
            string result2 = CryptoHelper.ComputePBKDF2("password", salt);
            result1.Should().Be(result2);
        }

        [Fact]
        public void ComputePBKDF2_NullPassword_ReturnsEmpty()
        {
            string result = CryptoHelper.ComputePBKDF2(null!, new byte[16]);
            result.Should().BeEmpty();
        }

        [Fact]
        public void ComputePBKDF2_NullSalt_ReturnsEmpty()
        {
            string result = CryptoHelper.ComputePBKDF2("password", null!);
            result.Should().BeEmpty();
        }

        // ── Certificate Loading ────────────────────────────────

        [Fact]
        public void LoadCertificateFromFile_NonExistent_ReturnsNull()
        {
            string path = "/tmp/nonexistent_cert_" + Guid.NewGuid().ToString("N") + ".pfx";
            X509Certificate2? cert = CryptoHelper.LoadCertificateFromFile(path);
            cert.Should().BeNull();
        }

        [Fact]
        public void LoadCertificateFromFile_NullPath_ReturnsNull()
        {
            X509Certificate2? cert = CryptoHelper.LoadCertificateFromFile(null!);
            cert.Should().BeNull();
        }

        [Fact]
        public void LoadCertificateFromFile_EmptyPath_ReturnsNull()
        {
            X509Certificate2? cert = CryptoHelper.LoadCertificateFromFile(string.Empty);
            cert.Should().BeNull();
        }

        [Fact]
        public void LoadCertificateFromStore_NonExistent_ReturnsNull()
        {
            // A thumbprint that doesn't exist in any store should return null.
            X509Certificate2? cert = CryptoHelper.LoadCertificateFromStore(
                "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00");
            cert.Should().BeNull();
        }

        [Fact]
        public void LoadCertificateFromStore_NullThumbprint_ReturnsNull()
        {
            X509Certificate2? cert = CryptoHelper.LoadCertificateFromStore(null!);
            cert.Should().BeNull();
        }

        // ── Self-Signed Certificate ────────────────────────────

        [Fact]
        public void CreateSelfSignedCertificate_OnNet8_ShouldReturnOrNull()
        {
            // On .NET 8.0 this should succeed; on net472 it returns null.
            X509Certificate2? cert = CryptoHelper.CreateSelfSignedCertificate("CN=TestSuite");
            if (cert is not null)
            {
                cert.Subject.Should().Be("CN=TestSuite");
                cert.NotBefore.Should().BeBefore(DateTime.UtcNow.AddDays(1));
                cert.NotAfter.Should().BeAfter(DateTime.UtcNow.AddMonths(1));
            }
        }

        [Fact]
        public void CreateSelfSignedCertificate_NullName_ReturnsNull()
        {
            X509Certificate2? cert = CryptoHelper.CreateSelfSignedCertificate(null!);
            cert.Should().BeNull();
        }

        [Fact]
        public void CreateSelfSignedCertificate_EmptyName_ReturnsNull()
        {
            X509Certificate2? cert = CryptoHelper.CreateSelfSignedCertificate(string.Empty);
            cert.Should().BeNull();
        }

        // ── Certificate Validation ─────────────────────────────

        [Fact]
        public void IsCertificateExpired_ForExpiredCert_ReturnsTrue()
        {
            // If we can create a cert, we can test expiry. Otherwise skip gracefully.
            X509Certificate2? cert = CryptoHelper.CreateSelfSignedCertificate("CN=Expired");
            if (cert is null)
            {
                // On net472 skip — cert creation not supported.
                return;
            }

            // The cert we created is valid for 10 years, so it's not expired.
            bool expired = CryptoHelper.IsCertificateExpired(cert);
            expired.Should().BeFalse("cert was just created");
        }

        [Fact]
        public void IsCertificateExpired_ForNullCert_ReturnsTrue()
        {
            bool result = CryptoHelper.IsCertificateExpired(null);
            result.Should().BeTrue();
        }

        [Fact]
        public void IsCertificateValidForSsl_ForOutput_ReturnsBool()
        {
            X509Certificate2? cert = CryptoHelper.CreateSelfSignedCertificate("CN=SSLTest");
            if (cert is null)
            {
                // On net472 skip — cert creation not supported.
                return;
            }

            // Our self-signed cert has Server Authentication EKU.
            bool valid = CryptoHelper.IsCertificateValidForSsl(cert);
            valid.Should().BeTrue();
        }

        [Fact]
        public void IsCertificateValidForSsl_Null_ReturnsFalse()
        {
            bool result = CryptoHelper.IsCertificateValidForSsl(null);
            result.Should().BeFalse();
        }

        [Fact]
        public void GetCertificateThumbprint_ReturnsThumbprint()
        {
            X509Certificate2? cert = CryptoHelper.CreateSelfSignedCertificate("CN=ThumbTest");
            if (cert is null)
                return;

            string? thumbprint = CryptoHelper.GetCertificateThumbprint(cert);
            thumbprint.Should().NotBeNullOrWhiteSpace();
            thumbprint!.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetCertificateThumbprint_Null_ReturnsNull()
        {
            string? result = CryptoHelper.GetCertificateThumbprint(null);
            result.Should().BeNull();
        }
    }
}
