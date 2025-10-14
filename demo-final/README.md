# Pluralsight Demo MCP Server

## Notes

- For accessing Azure, the demo uses `Azure.Identity` which supports multiple credential methods. The easiest method is using `az login` (Azure CLI) but there are other methods available.
    - If you use **Visual Studio Authentication**, you will need to define `AZURE_TENANT_ID` in the MCP Inspector environment variables and pass your tenant ID.
- For accessing Blob Containers, the demo uses Microsoft Entra ID authentication, [which requires the Blob Storage Reader role to be assigned](https://learn.microsoft.com/en-us/azure/storage/blobs/assign-azure-role-data-access?tabs=portal).
- For the demo user to access Azure AI Search, the roles **Azure Search Service Reader** is required and the AI Search service needs RBAC-based authentication enabled.