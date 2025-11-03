using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
using Globomantics.Mcp.Server.Documents;
using Globomantics.Mcp.Server.TimeOff;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestEase;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

// Configure user secrets and env vars for local development
builder.Configuration
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

// Configure all logs to go to stderr for MCP server
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithResourcesFromAssembly()
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly();

// Register HRM document service to connect to Azure Blob Storage
var azureCredential = new DefaultAzureCredential();
builder.Services
    .AddSingleton(_ => new BlobServiceClient(
        new Uri("https://psmcpdemo.blob.core.windows.net/"),
        azureCredential))
    .AddSingleton<IHrmDocumentService, HrmDocumentService>();

builder.Services.AddSingleton(_ => new SearchClient(
        new Uri("https://psdemo.search.windows.net"),
        "rag-globomantics-hrm",
        azureCredential));

builder.Services.AddSingleton(_ =>
{
    // For stdio, use client credential flow to call HRM API as MCP server identity
    var tenantId = builder.Configuration["AZURE_TENANT_ID"];
    var mcpClientId = builder.Configuration["MCP_SERVER_AAD_CLIENT_ID"];
    var mcpClientSecret = builder.Configuration["MCP_SERVER_AAD_CLIENT_SECRET"];
    var hrmEndpoint = builder.Configuration["HRM_API_ENDPOINT"];
    var hrmAppId = builder.Configuration["HRM_API_AAD_CLIENT_ID"];
    var hrmClientSecretCredential = new ClientSecretCredential(tenantId, mcpClientId, mcpClientSecret);

    return RestClient.For<IHrmAbsenceApi>(hrmEndpoint, async (request, cancellationToken) =>
            {
                var token = await hrmClientSecretCredential.GetTokenAsync(
                    new TokenRequestContext([$"api://{hrmAppId}/.default"]), cancellationToken);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
            });
});

var app = builder.Build();

await app.RunAsync();