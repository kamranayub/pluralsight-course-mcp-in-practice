# Main Demo

This is the final demo you can use for standing up the whole course demo infrastructure end-to-end.

## Infrastructure

To set up and provision all the infrastructure for this course, there's a mix of automation and manual steps outlined below:

1. Configure Microsoft Entra tenant
1. Provision HRM API
1. Configure HRM API
1. Configure AI Search Indexer
1. Provision MCP Server
1. Configure MCP Server

There are **two** Azure Bicep projects: `azure.yaml` and `Globomantics.Mcp.Server/azure.yaml`. These will provision **two separate resource groups** to maintain separation between the "mock infra" and the actual MCP server. They could be combined, if you want.

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