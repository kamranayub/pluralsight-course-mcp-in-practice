# HRM API

This is a mock demo API representing fictious HRM endpoints that is pre-deployed to Azure Functions that the MCP server can connect to. It is secured using "Easy Auth" and Microsoft Entra ID. It does not implement RBAC or authorization.

> [!WARNING]
> The demo API allows any user in the Entra tenant to authenticate. This should not be used as a reference example for production workloads.

## Prerequisites

- Visual Studio Code
- .NET 8 LTS
- Azure Functions Core Tools v4
- Azurite Extension
- _Optional:_ .NET Watch Attach Extension

## Starting

Press **F5** in Visual Studio Code to run **Attach to .NET Functions** Launch task.

## Deployment

Uses vanilla Azure Functions Core Tools in VS Code to deploy.

## Swagger

http://localhost:7071/api/swagger/ui

## Authentication

This demo uses the "easy flow" for authentication, delegating it all to [Azure Functions Built-in Authentication](https://learn.microsoft.com/en-us/azure/app-service/scenario-secure-app-authentication-app-service?tabs=workforce-configuration) with [Microsoft Entra](https://learn.microsoft.com/en-us/azure/app-service/configure-authentication-provider-aad). 

## Notes

- Reference schema example from: https://cookbook.openai.com/examples/chatgpt/gpt_actions_library/gpt_action_workday