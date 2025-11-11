using System.Text.Json;
using Maelstrom.Core;

// Create a new Maelstrom node
var node = new Node();

// Register a handler for "echo" messages
node.Handle("echo", async (Message msg) =>
{
    // Parse the message body as a JsonElement to work with it dynamically
    var bodyElement = msg.Body;
    
    // Create a response body dictionary
    var body = new Dictionary<string, object>();
    
    // Copy all fields from the request body
    foreach (var property in bodyElement.EnumerateObject())
    {
        // Convert JsonElement to appropriate object type
        body[property.Name] = property.Value.ValueKind switch
        {
            JsonValueKind.String => property.Value.GetString()!,
            JsonValueKind.Number => property.Value.GetInt32(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            _ => property.Value.GetRawText()
        };
    }

    // Update the message type to return back
    body["type"] = "echo_ok";

    // Echo the original message back with the updated message type
    await node.Reply(msg, body);
});

// Run the node
try
{
    await node.Run();
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync($"Error: {ex.Message}");
    await Console.Error.WriteLineAsync(ex.StackTrace);
    Environment.Exit(1);
}