# Module 2 - MCP Server with Tools, Resources, and Prompts (Stdio Transport)

This folder contains a C# MCP server that exposes tools, resources, and prompts that run against an Azure Functions HRM API and an Azure AI Search service. It uses the **Stdio** transport mode and includes an example of packaging a local MCP server using `dnx`.

## Prerequisites

- [.NET 8 SDK](https://get.dot.net/8)
- [.NET 10 SDK](https://get.dot.net/1o) for the `dnx` command

```sh
dotnet --list-sdks # Should include 10.x and 8.x
```

To compile the project, run `dotnet build`:

```sh
dotnet build
```

> [!CAUTION]
> This code **is not designed to be run without extra configuration and connected services.** It is provided 
> as a reference for what you see in the course module.

While it possible to run the MCP server in this folder, the tool calls will fail because they expect to connect to hosted services on the local machine or remotely hosted on Azure.

If you want to run the MCP server and play with it, the same exact server is implemented using the Streamable HTTP transport in the [everything-demo](../everything-demo/). That is designed so you _can_ run with a full local development environment where all the services are orchestrated and configured using Aspire, optionally using Azure or with OAuth protection.

## Distribution Using dnx

The course walks through how to deploy this MCP server using the `dotnet pack` and `dnx` commands.
You can see the configuration in the [project file](Globomantics.Mcp.Server\Globomantics.Mcp.Server.csproj).

> [!NOTE]
> The `dnx` command is only available in the .NET 10 SDK.

You can pack the project with the following command:

```sh
dotnet pack
```

In the `Globomantics.Mcp.Server\bin\Release` folder, you will see a list of `.nupkg` files, like this:

```sh
bin\Release\Pluralsight.Globomantics.Mcp.DemoServer.0.1.0-beta.nupkg
bin\Release\Pluralsight.Globomantics.Mcp.DemoServer.linux-arm64.0.1.0-beta.nupkg
...
```

Each package is for a different runtime platform such as Windows (64-bit) or Linux (Arm64).

> [!CAUTION]
> Please do not publish the NuGet package as-is since it's a demo server
> and not designed to be run publicly. You may copy the configuration
> to build and publish your own NuGet-based MCP server.

## Resources

- [MCP Servers in NuGet Packages](https://learn.microsoft.com/en-us/nuget/concepts/nuget-mcp)