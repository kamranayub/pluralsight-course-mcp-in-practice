# Model Context Protocol in Practice Pluralsight Course

This repository is for the Pluralsight course **[Model Context Protocol in Practice](https://www.pluralsight.com/courses/model-context-protocol-practice)**. The license is Apache 2.0.

[<img width="867" height="443" alt="image" src="https://github.com/user-attachments/assets/cb5e5e75-eaa9-44a6-a00a-74f0a4dd8345" />](https://www.pluralsight.com/courses/model-context-protocol-practice)

> Model Context Protocol (MCP) opens the door for connecting AI models to real systems. This course will teach you how to implement MCP servers that work with agentic tooling, both locally and remotely, and secure them using OAuth.

## Table of Contents

1. [Demos](#demos)
1. [Errata](#errata)
1. [Updates](#updates)
1. [Additional Resources](#additional-resources)

## Demos

This is an intermediate-level course about the practical aspects of building an MCP server with a real enterprise use case. 
It is not a step-by-step tutorial on how to build an MCP server end-to-end, since
the course is not designed to be around a specific tech stack like FastMCP, TypeScript, or even .NET. The MCP documentation and
various SDKs do a great job with basic tutorials and examples.

The demos are meant to be for reference and learning, not for running in production. That said, the full source code
is provided so you _can_ see how to build and deploy a remote MCP server on Azure.

### Prerequisites

- [Aspire 13.1+](https://aspire.dev) cross-platform development platform and orchestrator
  - _Required:_ [Docker](https://docker.com) or [Podman](https://podman.io/)
  - _Required:_ [.NET 8 and 10 SDKs](https://get.dot.net)
- [Node.js 22+](https://nodejs.org)
- [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- Visual Studio Code (recommended)
  - _Requires:_ [C# Devkit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
  - _Recommended:_ [Aspire extension](https://marketplace.visualstudio.com/items?itemName=microsoft-aspire.aspire-vscode)
  - _Recommended:_ [Azure Functions extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions)
  - _Recommended:_ [Azurite extension](https://learn.microsoft.com/en-us/azure/storage/common/storage-install-azurite) (recommended, for local storage emulation)

> [!IMPORTANT]
> In order to run some demos (M3 and M4), you will need an active [Microsoft Azure](https://azure.com) subscription and be logged in using the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest). See [how Aspire Azure Local Provisioning works](https://aspire.dev/integrations/cloud/azure/local-provisioning/).

**MCP Client Compatibility**

- From M1 to M3, the MCP server can be [connected to](https://modelcontextprotocol.io/docs/develop/connect-local-servers) by AI tools like VS Code, Claude, Cursor, and others.
- In M4, a Protected MCP server (OAuth) is introduced and is **only** compatible with VS Code due to restrictions with Microsoft Entra ID.

### How to Run

Each demo folder is meant to be opened in its own VS Code workspace, and has its own `README.md`
that explains the prerequisites required to run.

The demos use [Aspire](https://aspire.dev) to make it easier to provision a local cloud-native development
environment with fully orchestrated and connected services. It's like Docker Compose but code-first.

You can run every demo with a single command:

```sh
aspire run
```

However, each demo have have parameters or prerequisites before all the services will become :green_circle: Healthy.

### Folder Structure

Each module folder represents the end-state of the demo:

- **demo-full**: Fully provisioned Azure infrastructure and MCP server

Each module demo is designed to work locally:

- **demo-m1:** Basic scaffolded MCP server
- **demo-m2:** Local MCP server
- **demo-m3:** Local or Remote Azure Functions MCP server
- **demo-m4:** Protected MCP server with Entra ID

> [!IMPORTANT]
> Some tools like `AskAboutPolicy` and `PlanTimeOff` require an Azure AI search instance. The MCP server is
> designed to disable tools that don't have the required infrastructure provisioned. If you choose to
> use an Azure subscription, the required resources will be provisioned automatically for you.


## Errata

*None yet*

Please report course issues using the [Issues](issues) page or the Pluralsight discussion page.

## Updates

- **January 2026**
  - Fixed a typo causing `demo-final` to fail to build
  - Added Aspire-based demos for ease of use

- **December 2025**
  - Initial release 🎉

## Additional Resources

### Courses

- [Model Context Protocol: Getting Started](#) (**In Development**)
- [Model Context Protocol: Advanced Features](#) (**In Development**)
- [Using the Model Context Protocol SDK in C#](https://www.pluralsight.com/courses/using-c-model-context-protocol-sdk)

### Links

The PDF in the **Exercise Files** contains all the links used throughout the course.

### Documentation and Samples

- [MCP Specification](https://modelcontextprotocol.io/specification/2025-11-25)
- [MCP SDK List](https://modelcontextprotocol.io/docs/sdk)
  - Each SDK repository has many `examples` you can learn from and run locally
- [Build an MCP Server](https://modelcontextprotocol.io/docs/develop/build-server)
- [Connect to local MCP Servers](https://modelcontextprotocol.io/docs/develop/connect-local-servers)
- [Connect to remote MCP Servers](https://modelcontextprotocol.io/docs/develop/connect-remote-servers)