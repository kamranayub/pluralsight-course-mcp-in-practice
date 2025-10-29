# Globomantics MCP Server

## Commands

### `npm start`

Starts the .NET MCP server using `dotnet run` command.

### `npm run dev`

Starts the MCP inspector using the default `mcp.json` config file.

## Examples

### Ping Server

Copy and paste into `dotnet run` stdin:

```json
{"jsonrpc": "2.0", "id": 1, "method": "ping"}
```

## Client Compatibility

### Claude Desktop

- Does not support Resource Templates (have to expose a tool)

### Visual Studio Code

- Will not display resources until a tool is available

## Notes

- Do not use `Console.WriteLine` when using stdio transport. stdio messages must all be valid JSON-RPC messages. Either use the built-in .NET `ILogger` service redirected to stderr or `Console.Error.WriteLine` to log output.