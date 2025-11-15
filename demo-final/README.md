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

The `tenantId` or `AZURE_TENANT_ID` references are to your Entra tenant directory (aka Azure AD).

Follow the [Globomantics.Mcp.Server/README.md](Globomantics.Mcp.Server/README.md) file for Entra configuration steps.

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
