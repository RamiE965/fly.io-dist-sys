using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maelstrom.Core;

/// <summary>
/// Represents a message exchanged between nodes in the Maelstrom system.
/// </summary>
public class Message
{
    /// <summary>
    /// The source node ID (e.g., "c1", "n1")
    /// </summary>
    [JsonPropertyName("src")]
    public string Src { get; set; } = string.Empty;

    /// <summary>
    /// The destination node ID (e.g., "n1", "c1")
    /// </summary>
    [JsonPropertyName("dest")]
    public string Dest { get; set; } = string.Empty;

    /// <summary>
    /// The message body as a raw JSON element
    /// </summary>
    [JsonPropertyName("body")]
    public JsonElement Body { get; set; }
}