using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server;

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echo back the provided message")]
    public static string EchoMessage(
        [Description("The message to echo back")]
        string message,

        [Description("Number of times to repeat the message")]
        int repeat = 1)
    {
        for (var i = 0; i < repeat - 1; i++)
        {
            message += " " + message;
        }

        return $"You said: {message}";
    }
}