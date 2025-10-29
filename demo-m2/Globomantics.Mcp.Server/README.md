# Globomantics MCP Server

## Local Development

Set up for each client below:

<details>
<summary>VS Code</summary>

See `.vscode/mcp.json`

</details>

<details> 
<summary>Claude Desktop</summary>

**claude_desktop_config.json**

```
{
  "preferences": {
    "menuBarEnabled": false,
    "legacyQuickEntryEnabled": false
  },
  "mcpServers": {
    "globomantics-mcp-server": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "path/to/Globomantics.Mcp.Server/Globomantics.Mcp.Server.csproj"
      ]
    }
  }
}
```
</details>

> [!WARNING]
> Using `dotnet run` runs in Debug mode, which is the same output folder as `dotnet build` (`bin\Debug\net8.0`). You will get file lock/permission errors if you try to run both commands.
>
>  Using debug configuration is easier to attach to for debugging but you'll need to remember to either a) stop the MCP server manually in the client or b) exit the client completely (as in the case of Claude Desktop). You could also use the `-c Release` configuration flag to output to `bin\Release\net8.0` instead but you'll lose debug symbols.

## Commands

### `npm start`

Starts the .NET MCP server using `dotnet run` command.

### `npm run dev`

Starts the MCP inspector using the default `mcp.json` config file.


## Secrets

You will need to configure secrets to run this MCP server and have the required Azure resources set-up:

```sh
dotnet user-secrets init

# Azure Tenant ID (optional -- for Visual Studio credential auth)
dotnet user-secrets set "AZURE_TENANT_ID" "<tenant_id>"
# Azure Functions App Endpoint for HRM API
dotnet user-secrets set "HRM_API_ENDPOINT" "<endpoint>"
# Azure Entra application ID for the HRM API (EasyAuth)
dotnet user-secrets set "HRM_API_AAD_CLIENT_ID" "<client_id>"
# Azure Entra application ID for the MCP Server (S2S auth)
dotnet user-secrets set "MCP_SERVER_AAD_CLIENT_ID" "<client_id>"
# Azure Entra client secret credential value for MCP Server (S2S auth)
dotnet user-secrets set "MCP_SERVER_AAD_CLIENT_ID" "<client_secret>"
```

> [!WARNING]
> The .NET user secrets store is **not secure** and stores values in plain-text in your user profile folder.
> For production apps, you would want to [use something like Azure KeyVault](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-9.0)

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

#### Streamable HTTP transport

In the final demo, the MCP server uses a **native OAuth2 OBO flow** through Azure App Service Easy Auth and Entra ID. The server requests tokens **on behalf of the signed-in user** (the MCP client), effectively forwarding their credentials to the downstream HRM API in a standard native-app authorization flow.

To configure this, create an **App Registration** for the MCP server and grant it **delegated API permissions** to the HRM API (`user_impersonation` scope).

> [!TIP]
> Make sure you own both the MCP server and HRM API app registrations.
> Otherwise, the HRM API’s `user_impersonation` delegated permission won’t appear when you edit the MCP app registration.

## Examples

### Ping Server

Copy and paste into `dotnet run` stdin:

```json
{"jsonrpc": "2.0", "id": 1, "method": "ping"}
```

If there's a response, the server is working!

## Notes

- Do not use `Console.WriteLine` when using stdio transport. stdio messages must all be valid JSON-RPC messages. Either use the built-in .NET `ILogger` service redirected to stderr or `Console.Error.WriteLine` to log output.