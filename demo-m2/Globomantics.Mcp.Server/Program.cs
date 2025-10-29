using Azure.Identity;
using Azure.Storage.Blobs;
using Globomantics.Mcp.Server.Calendar;
using Globomantics.Mcp.Server.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithResources<CalendarResources>()
    .WithResources<DocumentResources>();

// Register HRM document service to connect to Azure Blob Storage
builder.Services
    .AddSingleton(_ => new BlobServiceClient(
        new Uri("https://psmcpdemo.blob.core.windows.net/"),
        new DefaultAzureCredential()))
    .AddSingleton<IHrmDocumentService, HrmDocumentService>();

var app = builder.Build();

await app.RunAsync();