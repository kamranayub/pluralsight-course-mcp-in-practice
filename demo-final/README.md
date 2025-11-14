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

### MCP Infra

In the `Globomantics.Mcp.Server` directory, you can run `azd up` with a unique environment.

Once provisioned, you must add the environment variables and the `HRM_API_ENDPOINT` should point to the HRM API Functions App provisioned above.
