using System;
using System.Collections.Generic;

namespace Iqbook.Runtime.Data
{
    [Serializable]
    public class IqbookMetadata
    {
        public string formatVersion = "1.0.0";
        public string bookId;
        public string title;
        public string author;
        public string manifestFile = "manifest.json";
        public string contentFile = "content.json";
        public string signatureFile = "signature.bin";
        public string publicKeyPem;
    }

    [Serializable]
    public class IqbookManifest
    {
        public string formatVersion = "1.0.0";
        public List<ManifestEntry> files = new();
    }

    [Serializable]
    public class ManifestEntry
    {
        public string path;
        public long size;
        public string sha256;
    }

    [Serializable]
    public class StoryContent
    {
        public List<StoryNode> nodes = new();
    }

    [Serializable]
    public class StoryNode
    {
        public string id;
        public string type;
        public string text;
        public string illustration;
        public string video;
        public string map;
        public List<Choice> choices = new();
    }

    [Serializable]
    public class Choice
    {
        public string text;
        public string next_node;
        public List<Condition> conditions = new();
    }

    [Serializable]
    public class Condition
    {
        public string flag;
        public bool value;
    }

    [Serializable]
    public class MapData
    {
        public string image;
        public List<MapZone> zones = new();
    }

    [Serializable]
    public class MapZone
    {
        public float[] rect;
        public string node;
    }
}
