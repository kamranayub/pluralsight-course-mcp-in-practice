#pragma warning disable ASPIREINTERACTION001 

using Aspire.Hosting.Azure;
using Aspire.Hosting.JavaScript;
using Azure.Identity;
using Azure.Provisioning;
using Azure.Provisioning.Storage;
using Azure.Storage.Blobs;
using Globomantics.Demo.AppHost.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;

var azureCredential = new DefaultAzureCredential();
var builder = DistributedApplication.CreateBuilder(args);

var enableAzureMode = builder.Configuration.GetValue("EnableAzure", false);
var enableMcpAuth = builder.Configuration.GetValue("EnableMcpAuth", false);

var hrmDocumentStorage = builder.AddAzureStorage("hrm-documents-storage")
    .ConfigureInfrastructure(infra =>
    {
        var storageAccount = infra.GetProvisionableResources()
            .OfType<StorageAccount>()
            .Single();

        infra.Add(new ProvisioningOutput("StorageAccountResourceId", typeof(string))
        {
            Value = storageAccount.Id
        });
    });

var api = builder.AddAzureFunctionsProject<Globomantics_Hrm_Api>("hrm-api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.ExecutionContext.IsPublishMode ? "Production" : "Development")
    .WithEnvironment("API_ENABLE_AUTH", enableMcpAuth.ToString())
    .WithExternalHttpEndpoints();

var hrmDocumentBlobs = hrmDocumentStorage
    .AddBlobContainer("hrm-blob-service", blobContainerName: "globomanticshrdocs")
    .OnResourceReady(async (resource, e, cancellationToken) =>
    {
        var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(resource);
        await resource.UploadDocumentsToStorageAsync(azureCredential, logger, cancellationToken);
    });

var mcp = builder.AddAzureFunctionsProject<Globomantics_Mcp_Server>("mcp")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.ExecutionContext.IsPublishMode ? "Production" : "Development")
    .WithEnvironment("MCP_ENABLE_AUTH", enableMcpAuth.ToString())
    .WithExternalHttpEndpoints()
    .WithReference(hrmDocumentBlobs)
    .WithReference(api)
    .WaitFor(api)
    .WaitFor(hrmDocumentStorage)
    .WaitFor(hrmDocumentBlobs)
    .OnResourceReady(async (resource, e, cancellationToken) =>
    {
        var interactionService = e.Services.GetRequiredService<IInteractionService>()!;

        if (!enableAzureMode)
        {
            _ = interactionService.PromptNotificationAsync(
                title: "Information",
                message: "Azure subscription has not been set, some MCP tools have been disabled. Refer to README for details.",
                options: new NotificationInteractionOptions
                {
                    Intent = MessageIntent.Information
                },
                cancellationToken: cancellationToken);
        }

        if (!enableMcpAuth)
        {

            _ = interactionService.PromptNotificationAsync(
                title: "Warning",
                message: "MCP Authentication is disabled. MCP server is operating in Anonymous mode and is not protected. Refer to README for details.",
                options: new NotificationInteractionOptions
                {
                    Intent = MessageIntent.Warning
                },
                cancellationToken: cancellationToken);

        }
    });

if (enableMcpAuth)
{
    builder.AddAuthMcpDemoResources(mcp, api);
}

if (enableAzureMode)
{
    builder
        .AddAzureDemoResources(
            azureCredential,
            mcp,
            api,
            hrmDocumentStorage,
            hrmDocumentBlobs)
        .AddCleanAzureResourcesStep();
}
else
{
    builder.AddCleanAzureNoopStep();
    hrmDocumentStorage.RunAsEmulator();
}

var mcpPatcher = builder
    .AddResource(new JavaScriptAppResource("mcp-inspector-entra-patch", "npx", ""))
    .WithNpm(install: true)
    .WithCommand("npm")
    .WithArgs("run", "patch:mcp")
    .ExcludeFromManifest();

var mcpInspector = builder.AddMcpInspector("mcp-inspector", options =>
{
    options.InspectorVersion = "0.18.0";
})
    .WithMcpServer(mcp, path: "/")
    .WaitForCompletion(mcpPatcher)
    .ExcludeFromManifest();

if (builder.ExecutionContext.IsRunMode) {
    mcp.WithReference(mcpInspector);
}

builder.Build().Run();

