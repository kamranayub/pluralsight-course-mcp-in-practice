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

If you want to compile and build the code, you will need:

- [.NET 8 and 10 SDKs](https://get.dot.net)
- [Node.js 22 or 24 LTS](https://nodejs.org)
- Visual Studio Code (recommended)
  - _Requires:_ [C# Devkit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
  - _Recommended:_ [Azure Functions extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions)

If you want to _run_ the demos, you will need:

**Azure Infrastructure**

- [A Microsoft Azure subscription](https://azure.com)
- [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azurite extension](https://learn.microsoft.com/en-us/azure/storage/common/storage-install-azurite) (recommended, for local storage emulation)
- A Microsoft Entra ID tenant (for OAuth flows)
- Azure Blob storage account
  - With uploaded PDFs from course materials
- Azure AI Foundry project
  - With a model deployment for `text-embedding-ada-002`
- Azure AI Search Service
  - Blob storage data source
  - Skillset
  - Search Index
  - Search Indexer
- Role-based security and managed identity setup

The details on infrastructure provisioning can be found in each module's `README.md`.

**MCP Client**

- From M1 to M3, the MCP server can be [connected to](https://modelcontextprotocol.io/docs/develop/connect-local-servers) by AI tools like VS Code, Claude, Cursor, and others.
- In M4, a Protected MCP server (OAuth) is introduced and is **only** compatible with VS Code due to restrictions with Microsoft Entra ID.

### Folder Structure

Each module folder represents the end-state of the demo:

- **demo-final**: Full Azure infrastructure and MCP server

The simplest demo to run:

- **demo-m1:** Basic scaffolded MCP server

The rest of the modules demo a C# MCP server running outside/within Azure:

- **demo-m2:** Local MCP server
- **demo-m3:** Deployed remote MCP server
- **demo-m4:** Protected MCP server

Each demo folder is meant to be opened in its own VS Code workspace.

> [!IMPORTANT]
> All the Azure-based MCP server demos **require a Microsoft Azure subscription** and
> additional resources to be provisioned in order to run. They are meant to be an enterprise-grade _reference_
> implementation, not necessarily demos you can run out-of-the-box. See the [Resources](#additional-resources)
> section below for smaller demos, samples, and documentation.

Each one has notes in the `README.md` files you can refer to if you want
to run the demos yourself.

## Errata

*None yet*

Please report course issues using the [Issues](issues) page or the Pluralsight discussion page.

## Updates

- **January 2026**
  - Fixed a typo causing `demo-final` to fail to build

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