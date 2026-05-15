using System.Text.Json.Serialization;

namespace Iqbook.Core;

public sealed class BookMetadata
{
    public required string Title { get; init; }
    public required string Author { get; init; }
    public required string FormatVersion { get; init; } = "1.0";
    public required string ContentHashHex { get; init; }
    public required string PublicKeyPem { get; init; }
    public bool IsEncrypted { get; init; }
    public string? EncryptionSaltB64 { get; init; }
    public string? EncryptionIvB64 { get; init; }
}

public sealed class StoryContent
{
    public required string StartNodeId { get; init; }
    public required List<Node> Nodes { get; init; } = new();
}

public sealed class Node
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public string? Text { get; init; }
    public string? Illustration { get; init; }
    public string? Video { get; init; }
    public string? Map { get; init; }
    public List<Choice> Choices { get; init; } = new();
}

public sealed class Choice
{
    public required string Text { get; init; }
    [JsonPropertyName("next_node")]
    public required string NextNode { get; init; }
    public string? ConditionFlag { get; init; }
    public bool ConditionValue { get; init; }
    public string? SetFlag { get; init; }
    public bool SetValue { get; init; }
}

public sealed class MapData
{
    public required string Image { get; init; }
    public required List<MapZone> Zones { get; init; } = new();
}

public sealed class MapZone
{
    public required float[] Rect { get; init; } // x,y,w,h in 0..1 or pixels
    public required string Node { get; init; }
}
