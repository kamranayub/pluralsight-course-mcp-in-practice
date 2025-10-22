using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Globomantics.Mcp.Server;
using Globomantics.Mcp.Server.TimeOff;
using Globomantics.Mcp.Server.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestEase;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithResourcesFromAssembly()
    // .WithTools(typeof(EchoTool))
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly()
    .WithCompleteHandler(Completions.CompleteHandler)
    .AddListPromptsFilter(Filters.ListPromptsFilter);

builder.Services.AddSingleton(_ => new BlobServiceClient(
        new Uri("https://psmcpdemo.blob.core.windows.net/"),
        new DefaultAzureCredential()));

builder.Services.AddSingleton<IHrmDocumentService, HrmDocumentService>();

// For stdio, use client credential flow to call HRM API as MCP server identity
builder.Services.AddSingleton(_ => RestClient.For<IHrmAbsenceApi>("https://globomanticshrmapi-bqhjgyb4e8fxc0gv.eastus-01.azurewebsites.net", async (request, cancellationToken) =>
            {
                var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
                var mcpClientId = Environment.GetEnvironmentVariable("MCP_SERVER_AAD_CLIENT_ID");
                var mcpClientSecret = Environment.GetEnvironmentVariable("MCP_SERVER_AAD_CLIENT_SECRET");
                var hrmAppId = Environment.GetEnvironmentVariable("HRM_API_AAD_CLIENT_ID");
                var scopes = new[] { $"api://{hrmAppId}/.default" };
                var credential = new ClientSecretCredential(tenantId, mcpClientId, mcpClientSecret);

                var token = await credential.GetTokenAsync(new TokenRequestContext(scopes), cancellationToken);

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
            }));

var app = builder.Build();

await app.RunAsync();
