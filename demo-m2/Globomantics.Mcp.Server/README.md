# Globomantics MCP Server

This is a sample MCP server for the Pluralsight course, MCP in Practice. It is not meant for public consumption and only
for demo purposes.

## Secrets

You will need to configure secrets to run this MCP server and have the required Azure resources set-up:

```sh
dotnet user-secrets init

# Azure Tenant ID (optional -- for Visual Studio credential auth)
dotnet user-secrets set "AZURE_TENANT_ID" "<tenant_id>"
# Azure Functions Endpoint for the HRM API
dotnet user-secrets set "HRM_API_ENDPOINT" "<hrm_endpoint_url>"
# Azure Entra application ID for the HRM API (EasyAuth)
dotnet user-secrets set "HRM_API_AAD_CLIENT_ID" "<client_id>"
# Azure Entra application ID for the MCP Server (S2S auth)
dotnet user-secrets set "MCP_SERVER_AAD_CLIENT_ID" "<client_id>"
# Azure Entra client secret credential value for MCP Server (S2S auth)
dotnet user-secrets set "MCP_SERVER_AAD_CLIENT_SECRET" "<client_secret>"
```

> [!WARNING]
> The .NET user secrets store is **not secure** and stores values in plain-text in your user profile folder.
> For production apps, you would want to [use something like Azure KeyVault](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-9.0)

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

## Publishing

Follow [guide on single-file apps](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview?tabs=cli) and [packaging MCP servers with NuGet](https://learn.microsoft.com/en-us/nuget/concepts/nuget-mcp). Reference the [AI templates `mcpserver`](https://github.com/dotnet/extensions/tree/8cc763f74f1bc551e361e481dac4c507b6dc905b/src/ProjectTemplates/Microsoft.Extensions.AI.Templates/src/McpServer/McpServer-CSharp) for an example template.

```sh
# Publish for local development
dotnet publish -r <runtime_identifier>

# e.g. Windows 64-bit
dotnet publish -r win-x64

# Pack for NuGet distribution
dotnet pack -c Release

# Release on NuGet (public)
cd bin/Release
dotnet nuget push "*.nupkg" --api-key <your-api-key> --source https://api.nuget.org/v3/index.json
```

## Notes

- Do not use `Console.WriteLine` when using stdio transport. stdio messages must all be valid JSON-RPC messages. Either use the built-in .NET `ILogger` service redirected to stderr or `Console.Error.WriteLine` to log output.