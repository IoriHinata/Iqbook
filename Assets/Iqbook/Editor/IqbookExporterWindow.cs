#if UNITY_EDITOR
using Iqbook.Runtime.Crypto;
using Iqbook.Runtime.Data;
using Iqbook.Runtime.IO;
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace Iqbook.Editor
{
    public class IqbookExporterWindow : EditorWindow
    {
        private string _sourceDir = "";
        private string _outputFile = "";
        private string _title = "New Book";
        private string _author = "Author";

        [MenuItem("Tools/IQBook/Exporter")]
        public static void ShowWindow()
        {
            GetWindow<IqbookExporterWindow>("IQBook Exporter");
        }

        private void OnGUI()
        {
            _sourceDir = EditorGUILayout.TextField("Source Folder", _sourceDir);
            _outputFile = EditorGUILayout.TextField("Output .iqbook", _outputFile);
            _title = EditorGUILayout.TextField("Title", _title);
            _author = EditorGUILayout.TextField("Author", _author);

            if (GUILayout.Button("Export Signed IQBook"))
            {
                Export();
            }
        }

        private void Export()
        {
            if (!Directory.Exists(_sourceDir)) throw new DirectoryNotFoundException(_sourceDir);
            using var rsa = RSA.Create(2048);
            var metadata = new IqbookMetadata
            {
                bookId = Guid.NewGuid().ToString("N"),
                title = _title,
                author = _author,
                publicKeyPem = rsa.ExportRSAPublicKeyPem()
            };

            new IqbookPackageService().BuildPackage(_sourceDir, _outputFile, metadata, rsa);
            Debug.Log($"IQBook exported: {_outputFile}");
        }
    }
}
#endif
