using System.Security.Claims;
using System.Text.Encodings.Web;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
using Globomantics.Mcp.Server;
using Globomantics.Mcp.Server.Auth;
using Globomantics.Mcp.Server.Documents;
using Globomantics.Mcp.Server.TimeOff;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using RestEase;

var builder = WebApplication.CreateBuilder(args);

builder.AddFunctionHostUrls();
builder.AddServiceDefaults();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowInspector", policy =>
    {
        policy
            .WithOrigins(builder.Configuration["MCP_INSPECTOR_CLIENT"]!)
            .WithMethods("GET", "POST", "DELETE", "OPTIONS")
            .WithHeaders("*")
            .WithExposedHeaders(
                "mcp-session-id",
                "last-event-id",
                "mcp-protocol-version")
            .AllowCredentials();
    });
});

builder.Services
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

        options.Events = new()
        {
            OnTokenValidated = context =>
            {
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
    .AddScheme<ScopedMcpAuthenticationOptions, ScopedMcpAuthenticationHandler>(McpAuthenticationDefaults.AuthenticationScheme, options =>
    {
        var tenantId = builder.Configuration["AZURE_TENANT_ID"];
        var aadOAuthServerUrl = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        var mcpClientId = builder.Configuration["MCP_SERVER_AAD_CLIENT_ID"];
        
        options.Scope = $"api://{mcpClientId}/user_impersonation";

        options.ResourceMetadata = new()
        {
            ResourceDocumentation = new Uri("https://globomantics.com/mcp"),
            AuthorizationServers = { new Uri(aadOAuthServerUrl) },
            
            ScopesSupported = [options.Scope],
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

// Allow accessing HttpContext in services
builder.Services.AddHttpContextAccessor();

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
app.MapDefaultEndpoints();

await app.RunAsync();

static void ConfigureHrmServices(IHostApplicationBuilder builder)
{
    // Configure Azure clients and services
    TokenCredential azureCredential;

    if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
    {
        azureCredential = new ManagedIdentityCredential(
            ManagedIdentityId.FromUserAssignedClientId(builder.Configuration["AZURE_CLIENT_ID"]));
    }
    else
    {
        // local development environment
        azureCredential = new DefaultAzureCredential();
    }

    builder.Services
        .AddSingleton(_ => new BlobServiceClient(
            new Uri(builder.Configuration["HRM_BLOB_SERVICE_URI"]!),
            azureCredential))
        .AddSingleton<IHrmDocumentService, HrmDocumentService>();

    builder.Services.AddSingleton(_ => new SearchClient(
            new Uri(builder.Configuration["HRM_SEARCH_SERVICE_URI"]!),
            builder.Configuration["HRM_SEARCH_SERVICE_INDEX_NAME"]!,
            azureCredential));

    builder.Services.AddSingleton(services =>
    {
        // This is using an OBO (On-Behalf-Of) flow to call the HRM API on behalf of the signed-in user
        // It does not use passwordless authentication or managed identity, but instead exchanges
        // the user's access token for a new access token to call the HRM API using the MCP client secret credential
        var tenantId = builder.Configuration["AZURE_TENANT_ID"];
        var mcpClientId = builder.Configuration["MCP_SERVER_AAD_CLIENT_ID"];
        var mcpClientSecret = builder.Configuration["MCP_SERVER_AAD_CLIENT_SECRET"];
        var hrmEndpoint = builder.Configuration["HRM_API_HTTP"];
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
}