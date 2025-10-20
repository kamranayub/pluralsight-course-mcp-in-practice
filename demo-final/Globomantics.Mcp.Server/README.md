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