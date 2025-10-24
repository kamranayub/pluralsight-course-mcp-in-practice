using RestEase;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Globomantics.Mcp.Server.TimeOff;
using Globomantics.Mcp.Server.Documents;
using Azure.Search.Documents;
using ModelContextProtocol.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Configure port for Azure Functions custom handler
var port = Environment.GetEnvironmentVariable("FUNCTIONS_CUSTOMHANDLER_PORT") ?? "5000";
var serverUrl = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") != null
    ? $"https://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}"
    : $"http://localhost:{port}";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Configure OAuth
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
})
.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var tenantId = builder.Configuration["AZURE_TENANT_ID"];
    var azureIssuerUrl = $"https://sts.windows.net/{tenantId}/";
    var mcpClientId = builder.Configuration["MCP_SERVER_AAD_CLIENT_ID"];

    options.Authority = azureIssuerUrl;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = azureIssuerUrl,
        ValidateAudience = true,
        ValidAudiences = [mcpClientId, $"api://{mcpClientId}"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        NameClaimType = "name",
        RoleClaimType = "roles"
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var authToken = context.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var name = context.Principal?.Identity?.Name ?? "unknown";
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email) ?? "unknown";
            Console.WriteLine($"Token validated for: {name} ({email})");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"Challenging client to authenticate with Entra ID");
            return Task.CompletedTask;
        }
    };
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
});;

// Add authorization support
builder.Services.AddAuthorization();

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
    // TODO: OBO flow
    var tenantId = builder.Configuration["AZURE_TENANT_ID"];
    var mcpClientId = builder.Configuration["MCP_SERVER_AAD_CLIENT_ID"];
    var mcpClientSecret = builder.Configuration["MCP_SERVER_AAD_CLIENT_SECRET"];
    var hrmAppId = builder.Configuration["HRM_API_AAD_CLIENT_ID"];
    var hrmClientSecretCredential = new ClientSecretCredential(tenantId, mcpClientId, mcpClientSecret);

    return RestClient.For<IHrmAbsenceApi>("https://globomanticshrmapi-bqhjgyb4e8fxc0gv.eastus-01.azurewebsites.net", async (request, cancellationToken) =>
            {
                var token = await hrmClientSecretCredential.GetTokenAsync(
                    new TokenRequestContext([$"api://{hrmAppId}/.default"]), cancellationToken);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
            });
});

builder.Services.AddSingleton<IHrmDocumentService, HrmDocumentService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowInspector");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/healthz", () => "Healthy");

app.MapMcp().RequireAuthorization();

await app.RunAsync();
