using RestEase;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Globomantics.Mcp.Server.TimeOff;
using Globomantics.Mcp.Server.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Azure.Search.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Configure user secrets and env vars for local development
builder.Configuration
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

// Configure all logs to go to stderr for MCP server
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Configure MCP server
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithResourcesFromAssembly()
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly();

// Configure Azure clients and services
var azureCredential = new DefaultAzureCredential();
builder.Services.AddSingleton(_ => new BlobServiceClient(
        new Uri("https://psmcpdemo.blob.core.windows.net/"),
        azureCredential));

builder.Services.AddSingleton(_ => new SearchClient(
        new Uri("https://psdemo.search.windows.net"),
        "rag-globomantics-hrm",
        azureCredential));

builder.Services.AddSingleton<IHrmDocumentService, HrmDocumentService>();

// For stdio, use client credential flow to call HRM API as MCP server identity
builder.Services.AddSingleton(_ => RestClient.For<IHrmAbsenceApi>("https://globomanticshrmapi-bqhjgyb4e8fxc0gv.eastus-01.azurewebsites.net", async (request, cancellationToken) =>
            {
                var tenantId = builder.Configuration["AZURE_TENANT_ID"];
                var mcpClientId = builder.Configuration["MCP_SERVER_AAD_CLIENT_ID"];
                var mcpClientSecret = builder.Configuration["MCP_SERVER_AAD_CLIENT_SECRET"];
                var hrmAppId = builder.Configuration["HRM_API_AAD_CLIENT_ID"];
                var scopes = new[] { $"api://{hrmAppId}/.default" };
                var credential = new ClientSecretCredential(tenantId, mcpClientId, mcpClientSecret);

                var token = await credential.GetTokenAsync(new TokenRequestContext(scopes), cancellationToken);

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
            }));

var app = builder.Build();

app.MapMcp();

await app.RunAsync();
