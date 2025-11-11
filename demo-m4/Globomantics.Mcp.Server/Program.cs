using System.Security.Claims;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
using Globomantics.Mcp.Server.Documents;
using Globomantics.Mcp.Server.TimeOff;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using ModelContextProtocol.AspNetCore.Authentication;
using RestEase;

var builder = WebApplication.CreateBuilder(args);

// Configure for Azure Functions custom handler
var port = Environment.GetEnvironmentVariable("FUNCTIONS_CUSTOMHANDLER_PORT") ?? "5000";
builder.WebHost.UseUrls($"http://localhost:{port}");

var serverUrl = builder.Environment.IsProduction() || builder.Environment.IsStaging()
    ? $"https://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}"
    : $"http://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowInspector", policy =>
    {
        policy
            .WithOrigins("http://localhost:6274") // MCP Inspector dev host
            .WithMethods("GET", "POST", "DELETE", "OPTIONS")
            .WithHeaders("*")                     // allow any headers
            .WithExposedHeaders(
                "mcp-session-id",
                "last-event-id",
                "mcp-protocol-version")
            .AllowCredentials();                  // optional if you later use cookies
    });
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddMcp(options =>
    {
        var tenantId = builder.Configuration["AZURE_TENANT_ID"];
        var aadOAuthServerUrl = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        var mcpClientId = builder.Configuration["MCP_SERVER_AAD_CLIENT_ID"];

        options.ResourceMetadata = new()
        {
            Resource = new Uri(serverUrl),
            ResourceDocumentation = new Uri("https://globomantics.com/mcp"),
            AuthorizationServers = { new Uri(aadOAuthServerUrl) },
            ScopesSupported = [$"api://{mcpClientId}/user_impersonation"],
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        // Compatibility with Azure Functions hosting model
        options.Stateless = true;
    })
    .WithResourcesFromAssembly()
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
      ForwardedHeaders.XForwardedHost |
      ForwardedHeaders.XForwardedProto;

    options.ForwardLimit = 1;
});

ConfigureHrmServices(builder);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowInspector");
}

app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();

app.MapMcp().RequireAuthorization();

app.MapGet("/api/healthz", () => "Healthy");

await app.RunAsync();

static void ConfigureHrmServices(IHostApplicationBuilder builder)
{
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
}