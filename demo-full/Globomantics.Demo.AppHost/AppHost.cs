using System.Reflection.Metadata;
using Aspire.Hosting.Azure;
using Aspire.Hosting.JavaScript;
using Aspire.Hosting.Pipelines;
using Azure;
using Azure.Identity;
using Azure.Provisioning;
using Azure.Provisioning.AppService;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Search;
using Azure.Provisioning.Storage;
using Azure.ResourceManager.Authorization.Models;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Storage.Blobs;
using Globomantics.Demo.AppHost.Roles;
using Globomantics.Demo.AppHost.Search;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;

var azureCredential = new DefaultAzureCredential();
var builder = DistributedApplication.CreateBuilder(args);

var hasAzureSubscriptionSet = builder.Configuration.GetValue<string>("Azure:SubscriptionId") is not null;

var azureTenantId = builder.AddParameter("azureTenantId", secret: true)
    .WithDescription("The Entra (Azure AD) Tenant ID that serves as the identity provider. For development, this can be the default tenant associated with your Azure account. Use `az account show --query tenantId` to discover the tenant ID.", enableMarkdown: true);

var enableMcpAuth = builder.AddParameter("enableMcpAuth", value: "false", publishValueAsDefault: true)
    .WithDescription("Whether or not to protect the MCP server with Entra ID. Requires additional Entra ID app registration configuration.");

var hrmApiAadClientId = builder.AddParameter("hrmApiAadClientId", value: "unset", secret: true)
    .WithDescription("The Entra (Azure AD) Client ID for the HRM API application.");

var mcpServerAadClientId = builder.AddParameter("mcpServerAadClientId", value: "unset", secret: true)
    .WithDescription("The Entra (Azure AD) Client ID for the MCP Server application.");

var mcpServerAadClientSecret = builder.AddParameter("mcpServerAadClientSecret", value: "unset", secret: true)
    .WithDescription("The Entra (Azure AD) Client Secret for the MCP Server application.");

var api = builder.AddAzureFunctionsProject<Globomantics_Hrm_Api>("hrm-api")
    .WithEnvironment("API_ENABLE_AUTH", enableMcpAuth)
    .WithEnvironment("HRM_API_AAD_CLIENT_ID", hrmApiAadClientId)
    .WithExternalHttpEndpoints()
    .PublishAsAzureAppServiceWebsite((infra, app) => app.Kind = "functionapp,linux");

if (hasAzureSubscriptionSet) {
    var appServicePlan = builder.AddAzureAppServiceEnvironment("ps-globomantics-aspire-ase")
        .ConfigureInfrastructure(infra =>
        {
            var resources = infra.GetProvisionableResources();
            var plan = resources.OfType<AppServicePlan>().Single();

            plan.Sku = new AppServiceSkuDescription
            {
                Name = "FC1",
                Tier = "FlexConsumption"
            };
        });
}

var hrmDocumentStorage = builder.AddAzureStorage("hrm-documents-storage")
    .RunAsEmulator()
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

var hrmDocumentBlobs = hrmDocumentStorage
    .AddBlobContainer("hrm-blob-service", blobContainerName: "globomanticshrdocs")
    .OnResourceReady(async (resource, e, cancellationToken) =>
    {
        var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(resource);
        var hrmDocumentConnString = await resource.GetConnectionProperty("ConnectionString").GetValueAsync(cancellationToken);
        var hrmDocumentBlobEndpoint = await resource
            .GetConnectionProperty("Uri").GetValueAsync(cancellationToken);

        logger.LogInformation("Retrieved blob endpoint: {Endpoint}", hrmDocumentBlobEndpoint);

        var blobServiceClient = hrmDocumentConnString == null
            ? new BlobServiceClient(new Uri(hrmDocumentBlobEndpoint!), azureCredential)
            : new BlobServiceClient(hrmDocumentConnString);

        // Upload PDFs
        var blobContainerName = await resource.GetConnectionProperty("BlobContainerName").GetValueAsync(cancellationToken);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);

        await blobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var pdfFiles = Directory.GetFiles("./documents", "*.pdf");

        foreach (var pdfFile in pdfFiles)
        {
            var filename = Path.GetFileName(pdfFile);
            var blobClient = blobContainerClient.GetBlobClient(filename);
            var fileInfo = await blobClient.UploadAsync(File.OpenRead(pdfFile), true, cancellationToken);

            logger.LogInformation("Uploaded blob {Filename} with content hash {ContentHash}", filename, fileInfo.Value.ContentHash);
        }
    });

var mcp = builder.AddAzureFunctionsProject<Globomantics_Mcp_Server>("mcp")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.ExecutionContext.IsPublishMode ? "Production" : "Development")
    .WithEnvironment("AZURE_TENANT_ID", azureTenantId)
    .WithEnvironment("MCP_ENABLE_AUTH", enableMcpAuth)
    .WithEnvironment("HRM_API_AAD_CLIENT_ID", hrmApiAadClientId)
    .WithEnvironment("MCP_SERVER_AAD_CLIENT_ID", mcpServerAadClientId)
    .WithEnvironment("MCP_SERVER_AAD_CLIENT_SECRET", mcpServerAadClientSecret)
    .WithExternalHttpEndpoints()
    .PublishAsAzureAppServiceWebsite((infra, app) => app.Kind = "functionapp,linux")
    .WithReference(hrmDocumentBlobs)
    .WithReference(api)
    .WaitFor(api)
    .WaitFor(hrmDocumentBlobs);

if (hasAzureSubscriptionSet) {

    var aiSearch = builder.AddAzureSearch("hrm-search-service")
        .WithRunIndexerCommand(azureCredential)
        .ConfigureInfrastructure(infra =>
        {
            var searchService = infra.GetProvisionableResources()
                                .OfType<SearchService>()
                                .Single();

            searchService.Identity = new ManagedServiceIdentity()
            {
                ManagedServiceIdentityType = ManagedServiceIdentityType.SystemAssigned
            };

            infra.Add(new ProvisioningOutput("SearchServicePrincipalId", typeof(string))
            {
                Value = searchService.Identity.PrincipalId!
            });
        })
        .OnResourceReady(async (resource, e, cancellationToken) =>
        {
            var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(resource);
            var searchServicePrincipalIdRaw = resource.Outputs["SearchServicePrincipalId"] as string;
            var searchServicePrincipalId = Guid.Parse(searchServicePrincipalIdRaw!);
            var storageAccountId = await hrmDocumentStorage.GetOutput("StorageAccountResourceId").GetValueAsync(cancellationToken);

            logger.LogInformation("Discovered Search Service Principal ID: {PrincipalId}", searchServicePrincipalId);
            logger.LogInformation("Discovered Storage Account Resource ID: {ResourceId}", storageAccountId);

            var blobDataReaderRole = Guid.Parse(StorageBuiltInRole.StorageBlobDataReader.ToString());

            await RoleAssignments.EnsureRoleAssignmentAsync(azureCredential, storageAccountId!, searchServicePrincipalId, blobDataReaderRole, cancellationToken);

            logger.LogInformation("RBAC granted: principal {PrincipalId} -> Storage Blob Data Reader on {Scope}", searchServicePrincipalId, storageAccountId);
        });

    var foundry = builder.AddAzureAIFoundry("hrm-foundry").ConfigureInfrastructure(infra =>
    {
        var resources = infra.GetProvisionableResources();
        var account = resources.OfType<Azure.Provisioning.CognitiveServices.CognitiveServicesAccount>().Single();

        infra.Add(new ProvisioningOutput("FoundryAccountResourceId", typeof(string))
        {
            Value = account.Id
        });

    }).OnResourceReady(async (resource, e, cancellationToken) =>
    {
        var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(resource);
        var searchServicePrincipalIdRaw = await aiSearch.GetOutput("SearchServicePrincipalId").GetValueAsync(cancellationToken);
        var searchServicePrincipalId = Guid.Parse(searchServicePrincipalIdRaw!);
        var foundryAccountId = resource.Outputs["FoundryAccountResourceId"]!.ToString();

        logger.LogInformation("Discovered Search Service Principal ID: {PrincipalId}", searchServicePrincipalId);
        logger.LogInformation("Discovered AI Foundry Resource ID: {ResourceId}", foundryAccountId);

        var openAiUserRole = Guid.Parse(CognitiveServicesBuiltInRole.CognitiveServicesOpenAIUser.ToString());

        await RoleAssignments.EnsureRoleAssignmentAsync(azureCredential, foundryAccountId!, searchServicePrincipalId, openAiUserRole, cancellationToken);

        logger.LogInformation("RBAC granted: principal {PrincipalId} -> OpenAI User on {Scope}", searchServicePrincipalId, foundryAccountId);
    });

    var embedding = foundry.AddDeployment("hrm-embeddings", AIFoundryModel.OpenAI.TextEmbeddingAda002)
        .WithProperties(conf =>
        {
            // The capacity needs to be set high enough for the search indexer
            // to index the PDF blob documents. By default it is set to 1k (~6 RPM)
            // so we set it to 10k (60 RPM).
            conf.SkuCapacity = 10;
        });

    mcp
        .WithEnvironment("HRM_SEARCH_SERVICE_INDEX_NAME", $"{aiSearch.Resource.Name}-index")
        .WithReference(aiSearch)
        .WaitFor(aiSearch)
        .WaitFor(foundry)
        .OnBeforeResourceStarted(async (resource, e, cancellationToken) =>
    {
        var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(resource);
        var config = e.Services.GetRequiredService<IConfiguration>();

        logger.LogInformation("Provisioning AI Search configuration for HRM data...");

        var searchEndpoint = await aiSearch.Resource.GetConnectionProperty("Uri").GetValueAsync(cancellationToken);
        var foundryEndpoint = await foundry.Resource.GetConnectionProperty("Uri").GetValueAsync(cancellationToken);
        var hrmDocumentBlobEndpoint = await hrmDocumentBlobs.Resource.GetConnectionProperty("Uri").GetValueAsync(cancellationToken);

        logger.LogDebug("Discovered AI Search endpoint: {Endpoint}", searchEndpoint);
        logger.LogDebug("Discovered AI Foundry endpoint: {Endpoint}", foundryEndpoint);
        logger.LogDebug("Discovered HRM Document Blob endpoint: {Endpoint}", hrmDocumentBlobEndpoint);

        var foundryUri = new Uri(foundryEndpoint!);
        var foundryResourceName = foundryUri.Host.Split('.').First();
        var storageAccountName = new Uri(hrmDocumentBlobEndpoint!).Host.Split('.').First();
        var blobContainerName = await hrmDocumentBlobs.Resource.GetConnectionProperty("BlobContainerName").GetValueAsync(cancellationToken);

        logger.LogDebug("Discovered HRM Document Blob Container Name: {BlobContainer}", blobContainerName);

        var indexClient = new SearchIndexClient(new Uri(searchEndpoint!), azureCredential);
        var indexerClient = new SearchIndexerClient(new Uri(searchEndpoint!), azureCredential);
        var storageAccountResourceId = await hrmDocumentStorage.GetOutput("StorageAccountResourceId").GetValueAsync(cancellationToken);
        var connectionString = $"ResourceId={storageAccountResourceId};";

        logger.LogDebug("Using Blob managed identity connection string: {ConnectionString}", connectionString);

        var dataSourceName = $"{aiSearch.Resource.Name}-datasource";
        var skillsetName =$"{aiSearch.Resource.Name}-skillset";
        var indexName = $"{aiSearch.Resource.Name}-index";

        var dataSource = await HrmSearchSteps.CreateOrUpdateSearchDataSource(logger, indexerClient, dataSourceName, blobContainerName!, connectionString);

        var skills = new List<SearchIndexerSkill>() {
            HrmSearchSteps.CreateSplitSkill(),
            HrmSearchSteps.CreateEmbeddingSkill(
                deploymentId: embedding.Resource.DeploymentName,
                modelName: embedding.Resource.ModelName,
                embeddingResourceUri: foundryUri
            )
        };
        
        var index = await HrmSearchSteps.CreateOrUpdateSearchIndex(
            logger, indexClient, indexName,
            embeddingDeploymentName: embedding.Resource.DeploymentName,
            embeddingModelName: embedding.Resource.ModelName,
            embeddingResourceUri: foundryUri
        );
        var skillset = await HrmSearchSteps.CreateOrUpdateSearchSkillSet(logger, indexerClient, skillsetName, skills, indexName);

        logger.LogInformation("Successfully provisioned AI Search index, vectorizer, skillset, and data source for HRM data.");
    });
}

var mcpEndpoint = mcp.GetEndpoint("http");

mcp.WithEnvironment("WEBSITE_HOSTNAME", ReferenceExpression.Create(
    $"{mcpEndpoint.Property(EndpointProperty.Host)}:{mcpEndpoint.Property(EndpointProperty.Port)}"));

var mcpPatcher = builder
    .AddResource(new JavaScriptAppResource("mcp-patcher", "npx", ""))
    .WithNpm(install: true)
    .WithCommand("npm")
    .WithArgs("run", "patch:mcp");

var mcpInspector = builder.AddMcpInspector("mcp-inspector", options =>
{
    options.InspectorVersion = "0.18.0";
})
    .WithMcpServer(mcp, path: "/")
    .WaitForCompletion(mcpPatcher);

mcp.WithReference(mcpInspector);

builder.Build().Run();