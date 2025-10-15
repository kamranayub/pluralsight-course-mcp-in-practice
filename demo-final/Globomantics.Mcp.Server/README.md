# Globomantics MCP Server

## Commands

### `npm start`

Starts the .NET MCP server using `dotnet run` command.

### `npm run dev`

Starts the MCP inspector using the default `mcp.json` config file.

### `npm run dev:az`

Starts the MCP inspector using a `mcp.azure.json` config file. Only use this if you want to fully replicate the demo environment shown in the course.

## Azure Configuration

For Microsoft Azure resources, you'll need to deploy the demo resources and have appropriate authentication.

For accessing Azure, the demo uses `Azure.Identity` which supports multiple credential methods. The easiest method is using `az login` (Azure CLI) but there are other methods available.
    - If you use **Visual Studio Authentication**, you will need to define `AZURE_TENANT_ID` in the MCP Inspector environment variables and pass your tenant ID in a MCP config file (e.g. `mcp.azure.json`).

For accessing Blob Containers, the demo uses Microsoft Entra ID authentication, [which requires the Blob Storage Reader role to be assigned](https://learn.microsoft.com/en-us/azure/storage/blobs/assign-azure-role-data-access?tabs=portal).

For the demo user to access Azure AI Search, the roles **Azure Search Service Reader** is required and the AI Search service needs RBAC-based authentication enabled.

## Examples

### Ping Server

Copy and paste into `dotnet run` stdin:

```json
{"jsonrpc": "2.0", "id": 1, "method": "ping"}
```

If there's a response, the server is working!

## Notes

- Do not use `Console.WriteLine` when using stdio transport. stdio messages must all be valid JSON-RPC messages. Either use the built-in .NET `ILogger` service redirected to stderr or `Console.Error.WriteLine` to log output.