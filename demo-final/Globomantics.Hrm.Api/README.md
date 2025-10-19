# HRM API

## Prerequisites

- Visual Studio Code
- .NET 8 LTS
- Azure Functions Core Tools v4
- Azurite Extension

## Starting

Press **F5** in Visual Studio Code.

## Swagger

http://localhost:7071/api/swagger/ui

## Authentication

This demo uses the "easy flow" for authentication, delegating it all to [Azure Functions Built-in Authentication](https://learn.microsoft.com/en-us/azure/app-service/scenario-secure-app-authentication-app-service?tabs=workforce-configuration) with [Microsoft Entra](https://learn.microsoft.com/en-us/azure/app-service/configure-authentication-provider-aad). It allows any user in the Entra tenant to authenticate. This should not be used as a reference example for production workloads.

## Notes

- Reference schema example from: https://cookbook.openai.com/examples/chatgpt/gpt_actions_library/gpt_action_workday