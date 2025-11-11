using System.Text.Json;

namespace Maelstrom.Core;

/// <summary>
/// Represents a Maelstrom node that handles incoming messages and sends replies.
/// This is the C# equivalent of the Go maelstrom.Node
/// </summary>
public class Node
{
    private readonly Dictionary<string, Func<Message, Task>> _handlers = new();
    private int _nextMessageId = 0;
    private readonly object _writeLock = new();

    /// <summary>
    /// The ID of this node (e.g., "n1", "n2")
    /// </summary>
    public string NodeId { get; private set; } = string.Empty;

    /// <summary>
    /// List of all node IDs in the cluster
    /// </summary>
    public List<string> NodeIds { get; private set; } = new();

    /// <summary>
    /// Registers a handler for a specific message type.
    /// </summary>
    public void Handle(string messageType, Func<Message, Task> handler)
    {
        _handlers[messageType] = handler;
    }

    /// <summary>
    /// Sends a reply to a received message.
    /// Automatically sets src, dest, msg_id, and in_reply_to fields.
    /// </summary>
    public async Task Reply(Message request, Dictionary<string, object> body)
    {
        // Get the original message ID to set as in_reply_to
        if (request.Body.TryGetProperty("msg_id", out var msgIdElement))
        {
            body["in_reply_to"] = msgIdElement.GetInt32();
        }

        // Set the message ID for this reply
        body["msg_id"] = GetNextMessageId();

        // Create the reply message
        var reply = new
        {
            src = NodeId,
            dest = request.Src,
            body = body
        };

        // Serialize and send
        await Send(reply);
    }

    /// <summary>
    /// Sends a message to another node.
    /// </summary>
    public async Task Send(object message)
    {
        var json = JsonSerializer.Serialize(message);
        
        // Lock to prevent interleaved writes
        lock (_writeLock)
        {
            Console.WriteLine(json);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs the node, continuously reading messages from STDIN and dispatching to handlers.
    /// </summary>
    public async Task Run()
    {
        string? line;
        while ((line = await Console.In.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            Message message;
            JsonDocument? doc = null;
            try
            {
                doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                
                message = new Message
                {
                    Src = root.GetProperty("src").GetString() ?? string.Empty,
                    Dest = root.GetProperty("dest").GetString() ?? string.Empty,
                    Body = root.GetProperty("body").Clone()
                };
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Failed to deserialize message: {ex.Message}");
                doc?.Dispose();
                continue;
            }
            finally
            {
                doc?.Dispose();
            }

            // Extract the message type
            if (!message.Body.TryGetProperty("type", out var typeElement))
            {
                await Console.Error.WriteLineAsync("Message missing 'type' field");
                continue;
            }

            var messageType = typeElement.GetString();
            if (string.IsNullOrEmpty(messageType))
            {
                await Console.Error.WriteLineAsync("Message 'type' field is empty");
                continue;
            }

            // Handle init message specially
            if (messageType == "init")
            {
                await HandleInit(message);
                continue;
            }

            // Look up and invoke the handler
            if (_handlers.TryGetValue(messageType, out var handler))
            {
                try
                {
                    await handler(message);
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"Handler error for '{messageType}': {ex.Message}");
                }
            }
            else
            {
                await Console.Error.WriteLineAsync($"No handler registered for message type: {messageType}");
            }
        }
    }

    private async Task HandleInit(Message message)
    {
        // Extract node_id and node_ids from the init message
        if (message.Body.TryGetProperty("node_id", out var nodeIdElement))
        {
            NodeId = nodeIdElement.GetString() ?? string.Empty;
        }

        if (message.Body.TryGetProperty("node_ids", out var nodeIdsElement))
        {
            NodeIds = JsonSerializer.Deserialize<List<string>>(nodeIdsElement.GetRawText()) 
                ?? new List<string>();
        }

        // Send init_ok reply
        var body = new Dictionary<string, object>
        {
            { "type", "init_ok" }
        };

        await Reply(message, body);
    }

    private int GetNextMessageId()
    {
        return Interlocked.Increment(ref _nextMessageId);
    }
}