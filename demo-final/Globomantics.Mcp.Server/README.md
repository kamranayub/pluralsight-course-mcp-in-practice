# Globomantics MCP Server

## Secrets

You will need to configure secrets to run this MCP server and have the required Azure resources set-up:

```sh
dotnet user-secrets init

# Azure Tenant ID (optional -- for Visual Studio credential auth)
dotnet user-secrets set "AZURE_TENANT_ID" "<tenant_id>"
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

- Ensure `http://localhost:6274/oauth/callback/debug` and `http://localhost:6274/oauth/callback` are added as Redirect URIs
- Ensure an API Permission scope is created for `api://{client_id}/user_impersonation` (which will allow delegated access)

> [!IMPORTANT]
> The demo uses a simplified (and less secure) OAuth flow that is compatible with Azure Entra ID and does not use Azure API Management. This is to keep the demos simpler and to focus on the MCP-specific implementation of OAuth.

> For a real production MCP server with Azure, the best practice would be to ensure **no Entra ID tokens** are sent back to the MCP client and to use passwordless flows using managed identities. For an advanced flow that demonstrates this, see [Den Delimarsky's sample and write-up](https://github.com/localden/remote-auth-mcp-apim-py) using APIM.
>
> In order to support the simpler flow, I had to [patch](patches/) the `@modelcontextprotocol/inspector` and `@modelcontextprotocol/inspector-client` packages, based on some work by Jeremy Smith (see [commits](https://github.com/modelcontextprotocol/inspector/compare/main...2underscores:inspector:azure-no-code-challenge-in-metadata) and [discussion](https://github.com/modelcontextprotocol/inspector/issues/685)).
>
> In addition, the OBO flow uses an client secret flow instead of passwordless auth because it's simpler. This is less secure
> since the `MCP_SERVER_AAD_CLIENT_SECRET` has to be provided in plain-text as an environment variable or user secret.

## Examples

### Ping Server

Copy and paste into `dotnet run` stdin:

```json
{"jsonrpc": "2.0", "id": 1, "method": "ping"}
```

If there's a response, the server is working!

## Notes

- Do not use `Console.WriteLine` when using stdio transport. stdio messages must all be valid JSON-RPC messages. Either use the built-in .NET `ILogger` service redirected to stderr or `Console.Error.WriteLine` to log output.
- The MCP server uses a custom Azure Functions host. By default, in .NET 8 isolated mode [Function Apps will have the ForwardedHeadersMiddleware enabled](https://github.com/Azure/azure-functions-host/issues/10253#issuecomment-3190530835) so you can safely use `Request.Scheme` and `Request.Host` which is [how the default MCP auth handler works](https://github.com/modelcontextprotocol/csharp-sdk/blob/8beafedddb937285645148cbd8daf15f783993bf/src/ModelContextProtocol.AspNetCore/Authentication/McpAuthenticationHandler.cs#L52), but since this uses a custom host, [we configure it manually](https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/8.0/forwarded-headers-unknown-proxies). This issue [has been documented with Duende IdentityServer as well](https://duendesoftware.com/blog/20250624-dotnet-8017-upgrades-forwarded-headers-and-unknown-proxy-issues).