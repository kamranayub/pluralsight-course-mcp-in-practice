# Main Demo

This is the final demo you can use for standing up the whole course demo infrastructure end-to-end.

## Infrastructure

There are two pieces to the infrastructure:

- **infra/** - The HRM "pre-existing" resources that the MCP server will connect to.
- **Globomantics.Mcp.Server/infra/** - The MCP server resources

These are **two separate environments** and resource groups.

### Entra Configuration

Follow the [GLobomantics.Mcp.Server/README.md] file for Entra configuration steps.

### HRM Infra

**Prerequisites**

- You must have a Microsoft Entra ID tenant set up
- You must create an App Registration for the Globomantics HRM client and MCP server clients
- You must have **both** the HRM API and MCP Server **Client IDs** available

1. At the root, you can run `azd up` and specify a unique environment name and location
    - Specify `aadHrmClientId` for HRM API
    - Specify `aadMcpClientId` for the MCP server
1. Once provisioned, you **must** configure the Entra ID (AAD) client secret in the Azure Functions App
    - Env Variable: `MICROSOFT_AUTHENTICATION_CLIENT_SECRET`

> [!IMPORTANT]
> The deployment will provision a free-tier AI Search service, but **not a model deployment** or an AI Search Indexer!

#### Azure AI Search Configuration

When you create an AI Search Service, Azure Portal has a few templated wizards for deploying a search indexer for Blob Containers.

Follow the wizard in the Azure Portal to hook up an AI Search Indexer with Azure Blob Storage.

> [!TIP]
> Walking through the wizard will have you create all the prerequisite model and AI Foundry resources.

**High-level Steps:**

1. Upload PDF files from this repository (`hrm-docs` folder) to the HRM document blob storage container (`globomanticshrm`)
1. Create an AI Foundry project with a model deployment for `text-embedding-ada-002` (I used `GlobalStandard`)
1. Create a Search Indexer created that is hooked up to the HRM Globomantics document blob storage container
1. Grant **Search Index Data Reader** permission for your demo employee/user identity (this can be yourself!)

### MCP Infra

In the `Globomantics.Mcp.Server` directory, you can run `azd up` with a unique environment.

Once provisioned, you must add the environment variables and the `HRM_API_ENDPOINT` should point to the HRM API Functions App provisioned above.
