# Full Demo

This is the full course demo project. It uses [Aspire](https://aspire.dev), a cross-platform Infrastructure-as-Code (IaC) local development environment.

## Prerequisites

- Follow the [Aspire](https://aspire.dev/get-started/prerequisites/) prerequisites guide
- [.NET 8 SDK](https://get.dot.net/8) and the [.NET 10 SDK](https://get.dot.net/10)
- [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Node.js 22+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or [Podman](https://podman.io)
- _Recommended:_ [Visual Studio Code](https://code.visualstudio.com)

The Aspire CLI (`aspire`) must be installed and available on the path. You can install using the following scripts.

On Windows:

    iex "& { $(irm https://aspire.dev/install.ps1) }"

On Linux or macOS:

    curl -sSL https://aspire.dev/install.sh | bash

Run the following commands to verify your environment is set up correctly:

```sh
dotnet --list-sdks # Should include 8.x and 10.x
node -v # Should be 22.x or above
npm -v # Should be 10.x or above
aspire --version # Should be 13.1 or above
func -v # Should be 4.6.0 or above
```

## Get Started

In the demo directory, run Aspire:

```sh
aspire run
```

> [!IMPORTANT]
> If this is your first time running an Aspire project, the `aspire run` command will prompt you to **Trust certificates**. On Windows and macOS, you can follow the prompts. These are required for the local development environment to use HTTPS.

If everything is working, you will Aspire print out the service information like this:

```sh

     AppHost:  Globomantics.Demo.AppHost/Globomantics.Demo.AppHost.csproj                 
                                                                                          
   Dashboard:  https://localhost:17006/login?t=unique-code        
                                                                                          
        Logs:  /Users/kamranicus/.aspire/cli/logs/apphost-10541-2026-01-09-16-26-56.log   
                                                            
               Press CTRL+C to stop the apphost and exit. 
```

Follow the **Dashboard** link to view the Aspire dashboard and find your service URLs.

The following Aspire resources should be **Healthy**:

- `mcp` - MCP server project (C#) hosted by default at `http://localhost:5000`
- `mcp-inspector` - MCP inspector (npx) hosted by default at `http://localhost:6274`
- `mcp-patcher` - Fixes a known issue with MCP Inspector that makes it incompatible with Entra ID-based OAuth flows
- `hrm-api` - Azure Functions project hosted on `http://localhost:7040`
- `hrm-documents-storage` - Azure blob storage (with PDFs pre-baked)
- `funcstorage...` - Azure Functions project backing storage

## Using the MCP Inspector

Aspire has provisioned the MCP Inspector (`mcp-inspector`) resource. Click the **Client** link in the Dashboard to connect to your MCP server!

## Connecting to the MCP Server

In your AI tool MCP configuration, you can follow the guide in the course or in tool documentation. The URL should be `http://localhost:5000`, but you
can view the MCP server URL in the Aspire Dashboard (the `mcp` resource).

### Visual Studio Code (default)

In the `.vscode/mcp.json` configuration, the MCP server is already set up:

```json
{
    "servers": {
        "globomantics-mcp-server-local": {            
            "type": "http",
            "url": "http://localhost:5000/"
        }
    }
}
```

Just click the **Start** command over the MCP server name to start it. Reference the course or [VS Code documentation](https://code.visualstudio.com/docs/copilot/customization/mcp-servers) for how to use with Copilot Agent mode.

### Claude Desktop (optional)

In the course, Claude is used to demo the MCP server for STDIO and Streamable HTTP transport. With authentication disabled, you can configure the MCP server in `claude_desktop_config.json` (Settings -> Developer):

```json
{
    "mcpServers": {
        "globomantics-mcp-server-local": {            
            "type": "http",
            "url": "http://localhost:5000/"
        }
    }
}
```

This is shown step-by-step in the course or you can [reference this guide by MCPBundles](https://www.mcpbundles.com/blog/claude-desktop-mcp#claude-desktop-mcp-config-file-location).

### ChatGPT (web only)

In order to use MCP servers with ChatGPT, you need to enable [Developer Mode](https://platform.openai.com/docs/guides/developer-mode).

> [!IMPORTANT]
> This is only available on the **Web** and on paid plans.

This requires you to deploy your MCP server, which you can find how to in the [Deploying the Project](#deploying-the-project) section.

> [!IMPORTANT]
> `enableAuth` must be `false` in your Aspire project as Entra ID is only supported by Visual Studio Code's MCP integration. In the **Advanced** MCP course,
> we introduce an Auth Gateway that makes your OAuth-protected MCP server compatible with all MCP clients.

## Azure Provisioning (optional)

By default, Aspire will not provision any Azure infrastructure and authentication is disabled. This is the easiest way to run the course demos
but some MCP tools that require Azure authentication, like `ask_about_policy` will be disabled. The rest will work locally!

You can optionally enable Azure provisioning to try out Azure AI Search or deploy the entire project remotely to run on Azure.

### Configure Azure Integration for Aspire

First, begin by logging into the Azure CLI:

```sh
az login
```

Choose your Azure subscription and note its `SubscriptionId`.

Then, add the following Azure subscription secrets:

```sh
dotnet user-secrets set "Azure:SubscriptionId" "your-subscription-id" --project ./Globomantics.Demo.AppHost
dotnet user-secrets set "Azure:Location" "eastus" --project ./Globomantics.Demo.AppHost
```

You will also need an Entra tenant ID, which can be the default tenant:

```sh
az account show --query tenantId
```

This will output your tenant ID quoted in a string, like `"<tenant_id>"`. Copy the tenant ID (without quotes) and set another user secret:

```sh
dotnet user-secrets set "Parameters:azureTenantId" "your-tenant-id" --project ./Globomantics.Demo.AppHost
```

> [!TIP]
> If you don't provide the `azureTenantId` parameter, Aspire will prompt you to add it in the Dashboard before
> any of the dependent resources will be started.

Now run Aspire:

```sh
aspire run
```

When provided an Azure subscription ID and an Entra tenant ID, Aspire will provision the following resources:

- `hrm-search-service` - Azure AI Search Service
- `hrm-foundry` - Azure AI Foundry
- `hrm-embeddings` - Foundry-deployed OpenAI model for text-embedding-ada-002

## Enabling Authentication (optional)



# Deploying the Project

> [!DANGER]
> This is not fully implemented. It requires custom Azure Bicep configuration that is not yet
> migrated to Aspire, but can be found in the `master` branch.

**Optionally,** you can deploy the MCP server and HRM API to Azure with Aspire:

```sh
aspire deploy
```

> [!WARNING]
> This will deploy Premium-level production resources and App Service plans to your Azure subscription.

## Infrastructure

To set up and provision all the infrastructure for this course, there's a mix of automation and manual steps outlined below:

1. Configure Microsoft Entra tenant
1. Provision HRM API
1. Configure HRM API
1. Configure AI Search Indexer
1. Provision MCP Server
1. Configure MCP Server

There are **two** Azure Bicep projects: `azure.yaml` and `Globomantics.Mcp.Server/azure.yaml`. These will provision **two separate resource groups** to maintain separation between the "mock infra" and the actual MCP server. They could be combined, if you want.

---

# Old Documentation (Not Yet Migrated)

> [!WARNING]
> The following docs have not yet been updated to reflect the Aspire project.

### Prerequisite: Entra Tenant Configuration

> [!TIP]
> You can reference [my Entra app manifest files](infra/entra/) (`infra/entra/`) to help verify your configuration.

> [!NOTE]
> The `tenantId` or `AZURE_TENANT_ID` references are to your Entra tenant directory (aka Azure AD).

1. Create an **App Registration** for the HRM API
    - Take note of the **App (Client) ID**
    - Add a `user_impersonation` API permission for **Delegated** auth
    - This is a simple setup -- the Azure Easy Auth will be configured during `azd up`
1. Create an **App Registration** for the MCP server
    - Add a Mobile/Desktop platform and ensure `ms-appx-web://microsoft.aad.brokerplugin/04f0c124-f2bc-4f59-8241-bf6df9866bbd` is added as a  Redirect URI
      - This is for `Azure.Identity` Broker plug-in
    - Add a SPA platform and ensure `http://localhost:6274/oauth/callback/debug` and `http://localhost:6274/oauth/callback` are added as Redirect URIs
      - This is for MCP Inspector support
    - Configure app delegation / impersonation configuration detailed below

#### Configuring App Delegation / Impersonation


Configure the MCP client app registration with [Native client app registration](https://learn.microsoft.com/en-us/azure/app-service/configure-authentication-provider-aad?tabs=workforce-configuration#native-client-application).

> [!IMPORTANT]
> The official documentation omits a key step:
> You must add your MCP app registration’s **Application (client) ID** to the **Allowed client applications** list in the Azure App Service Authentication settings.
> If this list is populated, Easy Auth blocks all callers that aren’t explicitly listed—including your MCP app—resulting in a 403 Forbidden response.
>
> **This is configured for you during `azd up`.**

Grant the MCP Server client app **delegated API permissions** to the HRM API (`user_impersonation` scope).

- Add an **API Permission scope** for `api://{client_id}/user_impersonation` (which will allow delegated access)

> [!TIP]
> Make sure you own both the MCP server and HRM API app registrations.
> Otherwise, the HRM API’s `user_impersonation` delegated permission won’t appear when you edit the MCP app registration.

> [!IMPORTANT]
> The demo uses a simplified (and less secure) OAuth flow that is compatible with Azure Entra ID and does not use Azure API Management. This is to keep the demos simpler and to focus on the MCP-specific implementation of OAuth.
>
> For a real production MCP server with Azure, the best practice would be to ensure **no Entra ID tokens** are sent back to the MCP client and to use passwordless flows using managed identities. For an advanced flow that demonstrates this, see [Den Delimarsky's sample and write-up](https://github.com/localden/remote-auth-mcp-apim-py) using APIM.
>
> In order to support the simpler flow, I had to [patch](patches/) the `@modelcontextprotocol/inspector` and `@modelcontextprotocol/inspector-client` packages, based on some work by Jeremy Smith (see [commits](https://github.com/modelcontextprotocol/inspector/compare/main...2underscores:inspector:azure-no-code-challenge-in-metadata) and [discussion](https://github.com/modelcontextprotocol/inspector/issues/685)).
>
> In addition, the OBO flow uses an client secret flow instead of passwordless auth because it's simpler. This is less secure
> since the `MCP_SERVER_AAD_CLIENT_SECRET` has to be provided in plain-text as an environment variable or user secret.


### Provision HRM API

**Resources Created:**

- App service plan
- Function app storage account
- HRM document storage account (`hrmdocs`)
- Log analytics workspace
- App insights
- HRM API Function app
  - Configured with Entra (AAD) authentication
  - Restricted to HRM/MCP client apps
  - Always authenticate; 302 Redirect for login

**Prerequisites**

- You must have a Microsoft Entra ID tenant set up
- You must create an App Registration for the Globomantics HRM client and MCP server clients
- You must have **both** the HRM API and MCP Server **Client IDs** available

1. At the root, you can run `azd up` and specify a unique environment name and location
    - Specify `aadHrmClientId` for HRM API
    - Specify `aadMcpClientId` for the MCP server
1. Once provisioned, you **must** configure the Entra ID (AAD) client secret in the Azure Functions App
    - Env Variable: `MICROSOFT_PROVIDER_AUTHENTICATION_SECRET`

> [!IMPORTANT]
> If `deployAiServices` is true, the deployment will provision a free-tier AI Search service, but **not a model deployment** or an AI Search Indexer!
> 
> You will still need to set these up manually as they cannot be provisioned through Bicep.

#### Upload PDF Documents

Before you can proceed to creating an AI Search Indexer, upload the PDF files from this repository (`hrm-docs` folder) 
to the storage account (`sthrmdocs`) under the `globomanticshrm` blob container.

#### Azure AI Search Configuration

When you create an AI Search Service, Azure Portal has a few templated wizards for deploying a search indexer for Blob Containers.

- Go to your AI Search Service
- Under **Overview** tab, select the toolbar item **Import Data (new)**
- Select **Azure Blob Storage**
- Follow the wizard and specify the `sthrmdocs`-prefixed storage account

> [!TIP]
> Walking through the wizard will have you create all the prerequisite model and AI Foundry resources.

**High-level Steps:**

1. Create an AI Foundry project with a model deployment for `text-embedding-ada-002` (I used `GlobalStandard`)
1. Create a Search Indexer created that is hooked up to the HRM Globomantics document blob storage container
1. RBAC: Grant yourself (owner) **Search Index Data Contributor** role
1. RBAC: Grant **Search Index Data Reader** permission for your demo employee(s) or group

### Provision MCP Server

In the `Globomantics.Mcp.Server` directory, you can run `azd up` with a unique environment.

Once provisioned, you must add the environment variables and the `HRM_API_ENDPOINT` should point to the HRM API Functions App provisioned above.

# Architecture

## HRM API

### Overview
- Implemented as Azure Functions (C# .NET) exposing a small HRM-compatible HTTP API.
- Uses OpenAPI attributes to annotate operations, parameters and responses so the function app can produce API documentation and client metadata.
- Lightweight, serverless design intended for demo / test usage backed by an in-memory MockDataStore.

### Authentication & identity
- Azure Functions App has "EasyAuth" enabled, which injects authenticted user principal in HTTP headers.
- Daemon aka S2S auth flow presents `Bearer` token in `Authorization` header for EasyAuth to authenticate (application-only authentication).
- Native app aka OBO (On-Behalf-Of) flow requires MCP client to authenticate and MCP server to forward credetials for impersonation (delegated access).
- The functions read claims from the incoming HttpRequest headers to identify the caller.
- The code extracts an email claim from the parsed ClaimsPrincipal and maps it to an Employee ID via the mock store. Missing/invalid authentication returns 401.

### Data & persistence
- Current implementation uses a MockDataStore in memory (Workers, AbsenceTypes, BenefitPlans, TimeOffRequests).
- Time off requests are appended in-memory and assigned a GUID for demonstration.
- Production guidance: replace MockDataStore with durable persistence (database, blob or managed service), and avoid in-memory state across function instances.

### Notes
- The API follows conventional RESTful structure for resource paths and HTTP verbs but embeds specific query conventions (e.g., Worker!Employee_ID and fixed category usage) to match the HRM integration surface.

## MCP Server

- The MCP server when run **locally** uses S2S (system identity) auth to talk to the HRM API and AI Search Service. This is the end-state of M3.
- The MCP server when run **remotely** uses OBO delegated auth. It is hosted on Azure Functions using the MCP Handler customization (and not the MCP Handler extension preview). This is the end-state of M4.

## Azure Blob Storage

The `sthrmdocs` Azure storage account contains a `globomanticshrm` Blob container. This container keeps several PDFs (found in repo). Each blob has metadata:

- **Description:** A brief LLM-friendly description of the document.
- **Category:** A `PlanDocumentCategory` string enum value (defined in MCP Server `HrmDocumentService` model).

The `Category` metadata is used to correlate a benefit document with benefit plan data from the HRM API. This is specific to the MCP server design.

## Azure AI Search

The search service exposes a search index that indexes the PDF documents for RAG vector search. It uses an embedding model and supports querying by text.