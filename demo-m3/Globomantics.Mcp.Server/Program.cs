using RestEase;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Globomantics.Mcp.Server.TimeOff;
using Globomantics.Mcp.Server.Documents;
using Azure.Search.Documents;

var builder = WebApplication.CreateBuilder(args);

// Configure port for Azure Functions custom handler
var port = Environment.GetEnvironmentVariable("FUNCTIONS_CUSTOMHANDLER_PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Configure MCP server
builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        // In the Advanced MCP course, we will discuss stateful vs stateless MCP servers
        options.Stateless = true;
    })
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

builder.Services.AddSingleton<IHrmDocumentService, HrmDocumentService>();

var app = builder.Build();

app.MapGet("/api/healthz", () => "Healthy");

app.MapMcp();

await app.RunAsync();
