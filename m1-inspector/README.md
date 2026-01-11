# Module 1 - MCP Server with MCP Inspector

In this demo, the server is scaffolded with the C# SDK and the MCP Inspector is configured. It uses `npm` as a package manager to wrap the `dotnet run` command for ease of use.

## Prerequisites

- [.NET 8 SDK](https://get.dot.net/8)
- [Node.js 22+](https://nodejs.org)

Run the following commands to verify your environment is set up correctly:

```sh
dotnet --list-sdks # Should include 8.x
node -v # Should be 22.x or above
npm -v # Should be 10.x or above
```

You will need to install the Node dependencies:

```sh
npm install
```

It should create a `node_modules` directory with the MCP Inspector package installed.

## Getting Started

In this demo directory, you can now run the following npm commands:

`npm start`

This will use .NET to start the MCP server using the stdio transport.

> [!TIP]
> This is the main command to run if you want to connect to the MCP server from an AI tool.

`npm run dev`

This will use `npx` to start the MCP Inspector and start the .NET MCP server as a child process to listen to the stdio transport.

> [!TIP]
> This is the command to run if you want to use the MCP Inspector to debug and test your MCP server.

## Using the MCP Inspector

Once started with `npm run dev`, it should launch your browser automatically. If it doesn't, you will see the following output in your terminal:

```sh
🚀 MCP Inspector is up and running at:
   http://localhost:6274/?MCP_PROXY_AUTH_TOKEN=a_very_long_token
```

Click that link or copy/paste into your browser to use the MCP Inspector, then click **Connect** to launch and connect to the MCP server.

## Connecting to the MCP Server

In your AI tool MCP configuration, you can follow the guide in the course or in tool documentation. You will need to set the command to use the `dotnet run` command and specify the working folder and project so that VS Code understands where the MCP server is located on your machine.

### Visual Studio Code (default)

In the `.vscode/mcp.json` configuration, the MCP server is already set up:

```json
{
    "servers": {
        "globomantics-mcp-server-local": {            
            "command": "npm",
            "args": ["run", "dev"],
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