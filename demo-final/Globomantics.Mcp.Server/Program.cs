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
            var authToken = context.HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
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

// Allow accessing HttpContext in services
builder.Services.AddHttpContextAccessor();

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
// This is using DefaultAzureCredential which uses the local developer identity
// and in production uses the managed identity assigned to the Azure Functions app
// (i.e. it does not perform actions or authenticate on behalf of the user)
var azureCredential = new DefaultAzureCredential();
builder.Services.AddSingleton(_ => new BlobServiceClient(
        new Uri("https://psmcpdemo.blob.core.windows.net/"),
        azureCredential));

builder.Services.AddSingleton(_ => new SearchClient(
        new Uri("https://psdemo.search.windows.net"),
        "rag-globomantics-hrm",
        azureCredential));

builder.Services.AddSingleton(services =>
{
    // This is using an OBO (On-Behalf-Of) flow to call the HRM API on behalf of the signed-in user
    // It does not use passwordless authentication or managed identity, but instead exchanges
    // the user's access token for a new access token to call the HRM API using the MCP client secret credential
    var tenantId = builder.Configuration["AZURE_TENANT_ID"];
    var mcpClientId = builder.Configuration["MCP_SERVER_AAD_CLIENT_ID"];
    var mcpClientSecret = builder.Configuration["MCP_SERVER_AAD_CLIENT_SECRET"];
    var hrmEndpoint = builder.Configuration["HRM_API_ENDPOINT"];
    var hrmAppId = builder.Configuration["HRM_API_AAD_CLIENT_ID"];

    return RestClient.For<IHrmAbsenceApi>(hrmEndpoint, async (request, cancellationToken) =>
            {
                var httpContext = services.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new InvalidOperationException("No HttpContext available to acquire user token.");
                var userAccessToken = httpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
                var hrmOboCredential = new OnBehalfOfCredential(tenantId, mcpClientId, mcpClientSecret, userAccessToken);

                var token = await hrmOboCredential.GetTokenAsync(
                    new TokenRequestContext([$"api://{hrmAppId}/user_impersonation"]), cancellationToken);
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
