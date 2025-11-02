# Globomantics MCP Server

This is a sample MCP server for the Pluralsight course, MCP in Practice. It is not meant for public consumption and only
for demo purposes.

For this module (**Building MCP Servers**), the server is a **Local** MCP server using STDIO transport. It is anonymous and allows any employee ID without verification or authentication.

In the next module, the server is migrated to **Streamable HTTP** and hosted remotely on Azure Functions.

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

## Azure Configuration

For Microsoft Azure resources, you'll need to deploy the demo resources and have appropriate authentication.

For accessing Azure, the demo uses `Azure.Identity` which supports multiple credential methods. The easiest method is using `az login` (Azure CLI) but there are other methods available.
    - If you use **Visual Studio Authentication**, you will need to define `AZURE_TENANT_ID` in the MCP Inspector environment variables and pass your tenant ID in a MCP config file (e.g. `mcp.azure.json`).

For accessing Blob Containers, the demo uses Microsoft Entra ID authentication, [which requires the Blob Storage Reader role to be assigned](https://learn.microsoft.com/en-us/azure/storage/blobs/assign-azure-role-data-access?tabs=portal).

For the demo user to access Azure AI Search, the roles **Search Index Data Reader** is required and the AI Search service needs RBAC-based authentication enabled.

### Auth

#### STDIO transport

When using the `stdio` transport, authentication options are limited. Enterprise scenarios that rely on **On-Behalf-Of (OBO)** flows require interactive user consent, which is only available through OAuth—supported by the **Streamable HTTP** transport.

For `stdio`, you can instead use **app-only authentication**, where the MCP server authenticates to an external API with its own client ID and secret. In this model, the server connects as *itself*, not as an individual user. This means endpoints that depend on a signed-in user won’t work until the demo is upgraded to the Streamable HTTP transport later in the course.

See [Native client app registration](https://learn.microsoft.com/en-us/azure/app-service/configure-authentication-provider-aad?tabs=workforce-configuration#native-client-application) for detailed setup steps.

> [!IMPORTANT]
> The official documentation omits a key step:
> You must add your MCP app registration’s **Application (client) ID** to the **Allowed client applications** list in the Azure App Service Authentication settings.
> If this list is populated, Easy Auth blocks all callers that aren’t explicitly listed—including your MCP app—resulting in a 403 Forbidden response.


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