using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Iqbook.Core;

public sealed class IqbookPackageService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public void CreatePackage(string sourceDir, string outputIqbookPath, string title, string author, string privatePem, string publicPem, string? encryptionPassword = null)
    {
        if (File.Exists(outputIqbookPath)) File.Delete(outputIqbookPath);

        var contentPath = Path.Combine(sourceDir, "content.json");
        if (!File.Exists(contentPath)) throw new FileNotFoundException("content.json not found", contentPath);

        var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories)
            .Select(p => (rel: Path.GetRelativePath(sourceDir, p).Replace('\\', '/'), abs: p))
            .Where(x => !x.rel.Equals("metadata.json", StringComparison.OrdinalIgnoreCase)
                     && !x.rel.Equals("signature.bin", StringComparison.OrdinalIgnoreCase)
                     && !x.rel.Equals("content.enc", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.rel)
            .ToList();

        string? saltB64 = null;
        string? ivB64 = null;
        byte[]? encryptedContent = null;

        if (!string.IsNullOrWhiteSpace(encryptionPassword))
        {
            var contentBytes = File.ReadAllBytes(contentPath);
            encryptedContent = Crypto.EncryptAes(contentBytes, encryptionPassword!, out var salt, out var iv);
            saltB64 = Convert.ToBase64String(salt);
            ivB64 = Convert.ToBase64String(iv);
        }

        // В манифест включаются именно те файлы, которые попадут в архив.
        var manifestBuilder = new StringBuilder();
        foreach (var f in files)
        {
            if (encryptedContent is not null && f.rel.Equals("content.json", StringComparison.OrdinalIgnoreCase))
            {
                var encHash = Crypto.Hex(Crypto.Sha256(encryptedContent));
                manifestBuilder.Append("content.enc").Append(':').Append(encHash).Append('\n');
                continue;
            }

            var hash = Crypto.Hex(Crypto.Sha256(File.ReadAllBytes(f.abs)));
            manifestBuilder.Append(f.rel).Append(':').Append(hash).Append('\n');
        }

        var manifestBytes = Crypto.Utf8(manifestBuilder.ToString());
        var contentHash = Crypto.Hex(Crypto.Sha256(manifestBytes));
        var signature = Crypto.SignSha256(manifestBytes, privatePem);

        var metadata = new BookMetadata
        {
            Title = title,
            Author = author,
            FormatVersion = "1.0",
            ContentHashHex = contentHash,
            PublicKeyPem = publicPem,
            IsEncrypted = encryptedContent is not null,
            EncryptionSaltB64 = saltB64,
            EncryptionIvB64 = ivB64
        };

        using var fs = File.Create(outputIqbookPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

        foreach (var f in files)
        {
            if (encryptedContent is not null && f.rel.Equals("content.json", StringComparison.OrdinalIgnoreCase))
            {
                var entry = zip.CreateEntry("content.enc", CompressionLevel.Optimal);
                using var es = entry.Open();
                es.Write(encryptedContent);
                continue;
            }

            var normal = zip.CreateEntry(f.rel, CompressionLevel.Optimal);
            using var n = normal.Open();
            using var input = File.OpenRead(f.abs);
            input.CopyTo(n);
        }

        var metadataEntry = zip.CreateEntry("metadata.json", CompressionLevel.Optimal);
        using (var ms = metadataEntry.Open())
        {
            var json = JsonSerializer.Serialize(metadata, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            ms.Write(bytes);
        }

        var sigEntry = zip.CreateEntry("signature.bin", CompressionLevel.Optimal);
        using (var ss = sigEntry.Open()) ss.Write(signature);
    }

    public LoadedBook LoadAndVerify(string iqbookPath, string? encryptionPassword = null)
    {
        using var fs = File.OpenRead(iqbookPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);

        var metadata = JsonSerializer.Deserialize<BookMetadata>(ReadEntry(zip, "metadata.json"))
            ?? throw new InvalidDataException("Invalid metadata.json");

        var signature = ReadEntryBytes(zip, "signature.bin");

        var entries = zip.Entries
            .Where(e => !e.FullName.Equals("metadata.json", StringComparison.OrdinalIgnoreCase)
                     && !e.FullName.Equals("signature.bin", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.FullName, StringComparer.Ordinal)
            .ToList();

        var manifestBuilder = new StringBuilder();
        foreach (var e in entries)
        {
            var hash = Crypto.Hex(Crypto.Sha256(ReadEntryBytes(zip, e.FullName)));
            manifestBuilder.Append(e.FullName).Append(':').Append(hash).Append('\n');
        }

        var manifestBytes = Crypto.Utf8(manifestBuilder.ToString());
        var calculatedHash = Crypto.Hex(Crypto.Sha256(manifestBytes));
        if (!string.Equals(calculatedHash, metadata.ContentHashHex, StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException("Content hash mismatch");

        if (!Crypto.VerifySha256(manifestBytes, signature, metadata.PublicKeyPem))
            throw new CryptographicException("Signature verification failed");

        string contentJson;
        if (metadata.IsEncrypted)
        {
            if (string.IsNullOrWhiteSpace(encryptionPassword))
                throw new UnauthorizedAccessException("Password required for encrypted content");

            var cipher = ReadEntryBytes(zip, "content.enc");
            var plain = Crypto.DecryptAes(cipher, encryptionPassword, Convert.FromBase64String(metadata.EncryptionSaltB64!), Convert.FromBase64String(metadata.EncryptionIvB64!));
            contentJson = Encoding.UTF8.GetString(plain);
        }
        else
        {
            contentJson = ReadEntry(zip, "content.json");
        }

        var story = JsonSerializer.Deserialize<StoryContent>(contentJson)
            ?? throw new InvalidDataException("Invalid content JSON");

        return new LoadedBook(metadata, story);
    }

    private static string ReadEntry(ZipArchive zip, string name)
    {
        var entry = zip.GetEntry(name) ?? throw new FileNotFoundException($"Entry not found: {name}");
        using var stream = entry.Open();
        using var sr = new StreamReader(stream, Encoding.UTF8);
        return sr.ReadToEnd();
    }

    private static byte[] ReadEntryBytes(ZipArchive zip, string name)
    {
        var entry = zip.GetEntry(name) ?? throw new FileNotFoundException($"Entry not found: {name}");
        using var stream = entry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}

public sealed record LoadedBook(BookMetadata Metadata, StoryContent Story);
