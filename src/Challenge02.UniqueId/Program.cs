using Maelstrom.Core;

var node = new Node();
int counter = 0;

node.Handle("generate", async (Message msg) =>
{
    int id = Interlocked.Increment(ref counter); // atomic increment
    
    string unqiueId = $"{node.NodeId}-{id}";
    
    var body = new Dictionary<string, object>
    {
        { "type", "generate_ok" },
        { "id", unqiueId }
    };
    
    await node.Reply(msg, body);
});

await node.Run();