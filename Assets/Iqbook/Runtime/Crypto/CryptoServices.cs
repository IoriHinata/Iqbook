using System;
using System.Security.Cryptography;
using System.Text;

namespace Iqbook.Runtime.Crypto
{
    public static class HashService
    {
        public static string Sha256Hex(byte[] bytes)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }

    public static class SignatureService
    {
        public static byte[] SignManifest(byte[] manifestBytes, RSA privateKey)
            => privateKey.SignData(manifestBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        public static bool VerifyManifest(byte[] manifestBytes, byte[] signature, RSA publicKey)
            => publicKey.VerifyData(manifestBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        public static RSA ImportPublicKeyPem(string pem)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa;
        }
    }

    public static class PasswordKeyService
    {
        public static byte[] DeriveKey(string password, byte[] salt, int iterations = 120000, int keySize = 32)
        {
            using var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            return kdf.GetBytes(keySize);
        }

        public static (byte[] Ciphertext, byte[] Iv) EncryptAes(byte[] plain, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            using var enc = aes.CreateEncryptor();
            return (enc.TransformFinalBlock(plain, 0, plain.Length), aes.IV);
        }

        public static byte[] DecryptAes(byte[] cipher, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using var dec = aes.CreateDecryptor();
            return dec.TransformFinalBlock(cipher, 0, cipher.Length);
        }
    }
}
