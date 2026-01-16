# Module 1 - Basic MCP Server

In this demo, the server is scaffolded with the C# SDK.

## Prerequisites

- [.NET 8 SDK](https://get.dot.net/8)

Run the following commands to verify your environment is set up correctly:

```sh
dotnet --list-sdks # Should include 8.x
```

## Getting Started

In this demo directory, run the `dotnet run` command to start the MCP server:

```sh
dotnet run
```

.NET will restore the Nuget packages and start the MCP server using the stdio transport.

## Examples

Once started, you can issue raw MCP JSON-RPC commands:

### Ping Server

Copy and paste into `dotnet run` stdin:

```json
{"jsonrpc": "2.0", "id": 1, "method": "ping"}
```

> [!NOTE]
> Do not use `Console.WriteLine` when using stdio transport. stdio messages must all be valid JSON-RPC messages. Either use the built-in .NET `ILogger` service redirected to stderr or `Console.Error.WriteLine` to log output.

## Connecting to the MCP Server

In your AI tool MCP configuration, you can follow the guide in the course or in tool documentation. You will need to set the command to use the `dotnet run` command and specify the working folder and project so that VS Code understands where the MCP server is located on your machine.

### Visual Studio Code (default)

In the `.vscode/mcp.json` configuration, the MCP server is already set up:

```json
{
    "servers": {
        "globomantics-mcp-server-local": {            
            "command": "dotnet",
            "args": [ 
                "run",
                "--project Globomantics.Mcp.Server/Globomantics.Mcp.Server.csproj"
            ],
            "cwd": "${workspaceFolder}"
        }
    }
}
```

Just click the **Start** command over the MCP server name to start it. Reference the course or [VS Code documentation](https://code.visualstudio.com/docs/copilot/customization/mcp-servers) for how to use with Copilot Agent mode.

If it is working, you will see the following MCP Server output logs in VS Code:

```
[warning] [server stderr] info: ModelContextProtocol.Server.StdioServerTransport[857250842]
[warning] [server stderr]       Server (stream) (Globomantics.Mcp.Server) transport reading messages.
[warning] [server stderr] info: Microsoft.Hosting.Lifetime[0]
[warning] [server stderr]       Application started. Press Ctrl+C to shut down.
```