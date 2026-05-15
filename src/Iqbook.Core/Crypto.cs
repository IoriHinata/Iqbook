using System.Security.Cryptography;
using System.Text;

namespace Iqbook.Core;

public static class Crypto
{
    public static (string privatePem, string publicPem) GenerateRsaPem(int bits = 3072)
    {
        using var rsa = RSA.Create(bits);
        return (rsa.ExportRSAPrivateKeyPem(), rsa.ExportRSAPublicKeyPem());
    }

    public static byte[] Sha256(byte[] data) => SHA256.HashData(data);

    public static byte[] SignSha256(byte[] data, string privatePem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privatePem);
        var hash = Sha256(data);
        return rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public static bool VerifySha256(byte[] data, byte[] signature, string publicPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicPem);
        var hash = Sha256(data);
        return rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public static byte[] EncryptAes(byte[] plain, string password, out byte[] salt, out byte[] iv)
    {
        salt = RandomNumberGenerator.GetBytes(16);
        using var kdf = new Rfc2898DeriveBytes(password, salt, 150_000, HashAlgorithmName.SHA256);
        var key = kdf.GetBytes(32);
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        iv = aes.IV;
        using var enc = aes.CreateEncryptor();
        return enc.TransformFinalBlock(plain, 0, plain.Length);
    }

    public static byte[] DecryptAes(byte[] cipher, string password, byte[] salt, byte[] iv)
    {
        using var kdf = new Rfc2898DeriveBytes(password, salt, 150_000, HashAlgorithmName.SHA256);
        var key = kdf.GetBytes(32);
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using var dec = aes.CreateDecryptor();
        return dec.TransformFinalBlock(cipher, 0, cipher.Length);
    }

    public static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
    public static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
}
