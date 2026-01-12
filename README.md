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
It is not a step-by-step how-to on how to build an MCP server end-to-end, since
that would depend on the specific tech stack like FastMCP, TypeScript, or .NET. The MCP documentation and
various SDKs do a great job with basic tutorials and examples.

The demos are meant to be for reference and learning, not for running in production. That said, the full source code
is provided so you _can_ see how to build and deploy a remote MCP server on Azure. You can run all the demos locally 
**without Azure or OAuth infrastructure.**

### Folder Structure

#### **Modules 3 and 4**

The [`everything-demo`](everything-demo) server is a full end-to-end local/remote MCP server that connects to a backend API and optionally, performs a RAG vector search with Azure AI Search.

This is the **main demo project** that contains all the tools, resources, and prompts shown in the course. It is designed to be configured
so that it reflects each module in different "modes", in increasing level of complexity:

- **Module 3:** Local Streamable HTTP MCP server
- **Module 3:** Remote MCP server deployed to Azure Functions
- **Module 4:** Remote MCP server deployed to Azure Functions and protected with Entra ID

Follow the [README.md](everything-demo/README.md) for how to configure and run the Everything Demo Server.

#### Other Demos

- **Module 1**
  - `m1-empty`: Empty scaffolded MCP server without tools
  - `m1-inspector`: Empty MCP server with Inspector configured
- **Module 2**
  - `m2-stdio`: Stdio transport MCP server with tools, resources, and prompts
  - `m2-token-counting`: The Anthropic token counting demo shown in the course

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