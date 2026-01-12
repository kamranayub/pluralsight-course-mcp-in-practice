using System.ComponentModel;
using ModelContextProtocol.Server;

public class EchoTool
{
    [McpServerTool, Description(
        """
        Echo back the provided message.

        - If the user provides a repeat count, the message is repeated that many times.
        - If they use exact quotes, then pass that as the message.
        - If there are multiple quotes, combine them into a single message.
        """)]
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