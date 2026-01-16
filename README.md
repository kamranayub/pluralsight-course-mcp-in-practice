# Model Context Protocol in Practice Pluralsight Course

<!-- TOC depthfrom:2 depthto:3 -->

- [Who Is This Course For?](#who-is-this-course-for)
- [The Everything Demo](#the-everything-demo)
    - [Features](#features)
- [Reference Materials](#reference-materials)
    - [Modules 3 and 4](#modules-3-and-4)
    - [Module 1](#module-1)
    - [Module 2](#module-2)
- [Errata](#errata)
- [Updates](#updates)
- [Additional Resources](#additional-resources)
    - [Courses](#courses)
    - [Links](#links)
    - [Documentation and Samples](#documentation-and-samples)

<!-- /TOC -->

This repository is for the Pluralsight course **[Model Context Protocol in Practice](https://www.pluralsight.com/courses/model-context-protocol-practice)**. The license is Apache 2.0.

[<img width="867" height="443" alt="image" src="https://github.com/user-attachments/assets/cb5e5e75-eaa9-44a6-a00a-74f0a4dd8345" />](https://www.pluralsight.com/courses/model-context-protocol-practice)

> Model Context Protocol (MCP) opens the door for connecting AI models to real systems. This course will teach you how to implement MCP servers that work with agentic tooling, both locally and remotely, and secure them using OAuth.

## Who Is This Course For?

This is an intermediate-level course for senior developers and architects about the practical aspects of building an MCP server with a real enterprise use case.

The course will answer questions you run into when deploying MCP to production within an enterprise context:

1. What do tools, resources, and prompts look like in practice?
1. What's the difference between stdio, Streamable HTTP, local, and remote servers?
1. How do you call downstream APIs?
1. How do you handle authentication and permissions?
1. How do you deal with hallucinations?
1. Why can't I just wrap my REST API and call it a day?
1. What does it mean to actually design _for_ both AI agents and human users?
1. How do clients work with MCP?

It poses these questions and uses the MCP .NET and C# SDK to help illustrate how you'd approach these problems. You'll watch me design a practical MCP server over the course of 90 minutes, with much of the code "filled in". The code is provided here in this repository for you to dive into.

This course **is not** for entry-level developers or beginners to MCP. It is not a step-by-step how-to guide on how to build an MCP server end-to-end, since that would depend on the specific tech stack like FastMCP, TypeScript, or .NET. 

If you are looking for entry-level courses, the MCP documentation and various SDKs do a great job with basic tutorials and examples. There are also Code Labs on Pluralsight and other MCP courses that dive deeper into specific MCP SDKs like FastMCP and C#. See the [Resources](#additional-resources) section below.

## The Everything Demo

Inside the [`everything-demo`](everything-demo) folder, you will find a production-grade MCP server using the MCP C# SDK with [Aspire](htttps://aspire.dev) tooling and deployable to Azure Container Apps (ACA). The course covers the high-level aspects of this but doesn't delve under the hood.

### Features

- Spins up a local development environment with [Aspire](htttps://aspire.dev)
- Supports provisioning Azure resources and configures them
- Runs locally using the Streamable HTTP transport
- Runs remotely hosted on Azure Container Apps (ACA)
- Supports OAuth with Entra ID (locally and remotely)
- Demonstrates delegated On-Behalf-Of (OBO) access to downstream API backend
- Demonstrates Azure AI vector search capability with PDF documents
- Demonstrates managed identity and role-based access control

## Reference Materials

The other folders are meant to be for reference and learning, not for running in production. They compile **but do not run.**

### **Modules 3 and 4**

The [`everything-demo`](everything-demo) server is a full end-to-end local/remote MCP server that connects to a backend API and optionally, performs a RAG vector search with Azure AI Search.

This is the **main demo project** that contains all the tools, resources, and prompts shown in the course. It is designed to be configured
so that it reflects each module in different "modes", in increasing level of complexity:

- **Module 3:** Local Streamable HTTP MCP server
- **Module 3:** Remote MCP server deployed to Azure Functions
- **Module 4:** Remote MCP server deployed to Azure Functions and protected with Entra ID

Follow the [README.md](everything-demo/README.md) for how to configure and run the Everything Demo Server.

### Module 1

- `m1-empty`: Empty scaffolded MCP server without tools
- `m1-inspector`: Empty MCP server with Inspector configured

### Module 2

- `m2-stdio`: Stdio transport MCP server with tools, resources, and prompts
- `m2-token-counting`: The Anthropic token counting demo shown in the course

## Errata

- **November 2025:** Version `2025-11-25` of the specification [added new capabilities](https://modelcontextprotocol.io/specification/2025-11-25/changelog) such as incremental scope consent and Client-issued Metadata Documents (CIMD) that are not yet widely supported by Identity Providers. Currently, Dynamic Client Registration (DCR) remains the most supported alongwith pre-registered clients. There is also experimental support for [durable tasks](https://modelcontextprotocol.io/specification/2025-11-25/basic/utilities/tasks). These are not covered in the course.

Please report course issues using the [Issues](issues) page or the Pluralsight discussion page.

## Updates

Please see the [GitHub Releases](releases) for more information.

- **January 2026**
  - Fixed a typo causing `demo-final` to fail to build
  - Added Aspire-based demos for ease of use
  - Enhanced READMEs of all the demos with more instructions and guides

- **December 2025**
  - Initial release 🎉

## Additional Resources

### Courses

- [Model Context Protocol Skill Path](https://app.pluralsight.com/paths/skill/model-context-protocol-mcp)
- [Model Context Protocol: Getting Started](#) (**In Development**)
- [Model Context Protocol: Advanced Features](#) (**In Development**)
- [Using the Model Context Protocol SDK in C#](https://www.pluralsight.com/courses/using-c-model-context-protocol-sdk)
- [FastMCP Foundations](https://www.pluralsight.com/courses/fastmcp-foundations1)

### Links

The PDF in the **Exercise Files** contains all the links used throughout the course.

### Documentation and Samples

- [MCP Specification](https://modelcontextprotocol.io/specification/2025-11-25)
- [MCP SDK List](https://modelcontextprotocol.io/docs/sdk)
  - Each SDK repository has many `examples` you can learn from and run locally
- [Build an MCP Server](https://modelcontextprotocol.io/docs/develop/build-server)
- [Connect to local MCP Servers](https://modelcontextprotocol.io/docs/develop/connect-local-servers)
- [Connect to remote MCP Servers](https://modelcontextprotocol.io/docs/develop/connect-remote-servers)
