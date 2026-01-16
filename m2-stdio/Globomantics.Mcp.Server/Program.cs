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

var hrmBlobServiceConnectionString = builder.Configuration["HRM_BLOB_SERVICE_CONNECTIONSTRING"];

if (hrmBlobServiceConnectionString is not null)
{
    builder.Services
        .AddSingleton(_ => new BlobServiceClient(hrmBlobServiceConnectionString));
} 
else 
{
    builder.Services
        .AddSingleton(_ => new BlobServiceClient(
            new Uri(builder.Configuration["HRM_BLOB_SERVICE_URI"]!),
            azureCredential));
}

builder.Services.AddSingleton<IHrmDocumentService, HrmDocumentService>();

builder.Services.AddSingleton(_ => new SearchClient(
        new Uri(builder.Configuration["HRM_SEARCH_SERVICE_URI"]!),
        builder.Configuration["HRM_SEARCH_SERVICE_INDEX_NAME"]!,
        azureCredential));

builder.Services.AddSingleton(services =>
{
    var hrmEndpoint = builder.Configuration["HRM_API_HTTP"];
    return RestClient.For<IHrmAbsenceApi>(hrmEndpoint);
});

var app = builder.Build();

await app.RunAsync();