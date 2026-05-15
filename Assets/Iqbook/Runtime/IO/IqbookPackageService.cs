using Iqbook.Runtime.Crypto;
using Iqbook.Runtime.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Iqbook.Runtime.IO
{
    public class IqbookPackageService
    {
        public IqbookMetadata LoadAndValidate(string packagePath, out StoryContent content)
        {
            content = null;
            using var archive = ZipFile.OpenRead(packagePath);
            var metadata = ReadJson<IqbookMetadata>(archive, "metadata.json");
            var manifestEntry = archive.GetEntry(metadata.manifestFile) ?? throw new InvalidDataException("manifest missing");
            var contentEntry = archive.GetEntry(metadata.contentFile) ?? throw new InvalidDataException("content missing");
            var sigEntry = archive.GetEntry(metadata.signatureFile) ?? throw new InvalidDataException("signature missing");

            var manifestBytes = ReadBytes(manifestEntry);
            var signatureBytes = ReadBytes(sigEntry);

            using var pub = SignatureService.ImportPublicKeyPem(metadata.publicKeyPem);
            if (!SignatureService.VerifyManifest(manifestBytes, signatureBytes, pub))
                throw new CryptographicException("Invalid iqbook signature.");

            var manifest = JsonUtility.FromJson<IqbookManifest>(Encoding.UTF8.GetString(manifestBytes));
            ValidateFileHashes(archive, manifest);

            content = JsonUtility.FromJson<StoryContent>(Encoding.UTF8.GetString(ReadBytes(contentEntry)));
            return metadata;
        }

        public void BuildPackage(string sourceDir, string outputPath, IqbookMetadata metadata, RSA privateKey)
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
            var manifest = BuildManifest(sourceDir);
            var manifestBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(manifest, true));
            var signature = SignatureService.SignManifest(manifestBytes, privateKey);

            using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);
            WriteString(archive, "metadata.json", JsonUtility.ToJson(metadata, true));
            WriteString(archive, metadata.manifestFile, Encoding.UTF8.GetString(manifestBytes));
            WriteBytes(archive, metadata.signatureFile, signature);

            foreach (var entry in manifest.files)
            {
                var fullPath = Path.Combine(sourceDir, entry.path);
                archive.CreateEntryFromFile(fullPath, entry.path, CompressionLevel.Optimal);
            }
        }

        private static IqbookManifest BuildManifest(string sourceDir)
        {
            var manifest = new IqbookManifest();
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .OrderBy(f => f.FullName, StringComparer.Ordinal);

            foreach (var file in files)
            {
                var relPath = Path.GetRelativePath(sourceDir, file.FullName).Replace('\\', '/');
                var bytes = File.ReadAllBytes(file.FullName);
                manifest.files.Add(new ManifestEntry { path = relPath, size = file.Length, sha256 = HashService.Sha256Hex(bytes) });
            }
            return manifest;
        }

        private static void ValidateFileHashes(ZipArchive archive, IqbookManifest manifest)
        {
            foreach (var m in manifest.files)
            {
                var e = archive.GetEntry(m.path) ?? throw new InvalidDataException($"Missing file: {m.path}");
                var bytes = ReadBytes(e);
                var hash = HashService.Sha256Hex(bytes);
                if (!string.Equals(hash, m.sha256, StringComparison.OrdinalIgnoreCase) || bytes.LongLength != m.size)
                    throw new CryptographicException($"Hash/size mismatch: {m.path}");
            }
        }

        private static T ReadJson<T>(ZipArchive archive, string entryName)
        {
            var entry = archive.GetEntry(entryName) ?? throw new InvalidDataException($"Missing file: {entryName}");
            return JsonUtility.FromJson<T>(Encoding.UTF8.GetString(ReadBytes(entry)));
        }

        private static byte[] ReadBytes(ZipArchiveEntry e)
        {
            using var s = e.Open();
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }

        private static void WriteString(ZipArchive archive, string path, string value)
            => WriteBytes(archive, path, Encoding.UTF8.GetBytes(value));

        private static void WriteBytes(ZipArchive archive, string path, byte[] bytes)
        {
            var e = archive.CreateEntry(path, CompressionLevel.Optimal);
            using var s = e.Open();
            s.Write(bytes, 0, bytes.Length);
        }
    }
}
