#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES002
#pragma warning disable ASPIREUSERSECRETS001
#pragma warning disable ASPIREINTERACTION001

using System.Text.Json.Nodes;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Pipelines;
using Azure.Core;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Search;
using Azure.Provisioning.Storage;
using Azure.ResourceManager.Authorization.Models;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Globomantics.Demo.AppHost.Azure;
using Globomantics.Demo.AppHost.Roles;
using Globomantics.Demo.AppHost.Search;
using Globomantics.Demo.AppHost.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class AppHostMcpDemoExtensions
{
    public static IDistributedApplicationBuilder AddAuthMcpDemoResources(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<AzureFunctionsProjectResource> mcp,
        IResourceBuilder<AzureFunctionsProjectResource> hrmApi)
    {
        var azureTenantId = builder.AddParameter("azureTenantId", secret: true)
            .WithDescription("The Entra (Azure AD) Tenant ID for the Azure resources. You can find this using the Azure CLI command: `az account show --query tenantId`", enableMarkdown: true);

        var hrmApiAadClientId = builder.AddParameter("hrmApiAadClientId", secret: true)
            .WithDescription("The Entra (Azure AD) Client ID for the HRM API application.");

        var hrmApiAadClientSecret = builder.AddParameter("hrmApiAadClientSecret", secret: true)
            .WithDescription("The Entra (Azure AD) Client Secret for the HRM API application.");

        var mcpServerAadClientId = builder.AddParameter("mcpServerAadClientId", secret: true)
            .WithDescription("The Entra (Azure AD) Client ID for the MCP Server application.");

        var mcpServerAadClientSecret = builder.AddParameter("mcpServerAadClientSecret", secret: true)
            .WithDescription("The Entra (Azure AD) Client Secret for the MCP Server application.");

        mcp
            .WithEnvironment("AZURE_TENANT_ID", azureTenantId)
            .WithEnvironment("HRM_API_AAD_CLIENT_ID", hrmApiAadClientId)
            .WithEnvironment("MCP_SERVER_AAD_CLIENT_ID", mcpServerAadClientId)
            .WithEnvironment("MCP_SERVER_AAD_CLIENT_SECRET", mcpServerAadClientSecret);

        hrmApi
            .WithEnvironment("HRM_API_AAD_CLIENT_ID", hrmApiAadClientId)
            .WithEnvironment("WEBSITE_AUTH_AAD_ALLOWED_TENANTS", azureTenantId)
            .WithEnvironment("MICROSOFT_PROVIDER_AUTHENTICATION_SECRET", hrmApiAadClientSecret);

        // See: https://learn.microsoft.com/en-us/azure/container-apps/authentication-entra
        // See: https://learn.microsoft.com/en-us/azure/container-apps/authentication#secure-endpoints-with-easyauth
        builder.Pipeline.AddStep($"update-{hrmApi.Resource.Name}-microsoft-auth", async (context) =>
        {
            var deploymentStateManager = context.Services.GetRequiredService<IDeploymentStateManager>();
            var azureDeploymentConfig = await deploymentStateManager.AcquireSectionAsync("Azure");
            var resourceGroupName = azureDeploymentConfig.Data["ResourceGroup"]!.GetValue<string>();

            var tenantId = await azureTenantId.Resource.GetValueAsync(context.CancellationToken);
            var clientId = await hrmApiAadClientId.Resource.GetValueAsync(context.CancellationToken);
            string[] allowedAudiences = [$"{clientId}", $"api://{clientId}", $"api://{clientId}/user_impersonation"];

            var configureAuthTask = await context.ReportingStep
                    .CreateTaskAsync($"Configuring Microsoft Entra authentication for {hrmApi.Resource.Name} ACA resource", context.CancellationToken)
                    .ConfigureAwait(false);

            await using (configureAuthTask.ConfigureAwait(false))
            {
                try
                {

                    await AzCliCommands.ConfigureContainerAppAuthWithMicrosoft(hrmApi.Resource.Name, resourceGroupName, tenantId!, clientId!, allowedAudiences, context.CancellationToken).ConfigureAwait(false);
                    var containerAppEndpoint = await AzCliCommands.GetContainerAppEndpoint(hrmApi.Resource.Name, resourceGroupName, clientId!, context.CancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Failed to retrieve container app endpoint");
                    await AzCliCommands.ConfigureContainerAppAuthRedirectUri(containerAppEndpoint, clientId!, context.CancellationToken).ConfigureAwait(false);

                    await configureAuthTask.CompleteAsync(
                        $"Successfully configured Microsoft Entra authentication for {hrmApi.Resource.Name} ACA resource.",
                        CompletionState.Completed,
                        context.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await configureAuthTask.CompleteAsync(
                        $"Error configuring Microsoft Entra authentication: {ex.Message}",
                        CompletionState.CompletedWithError,
                        context.CancellationToken).ConfigureAwait(false);
                }
            }
        }, requiredBy: WellKnownPipelineSteps.Deploy, dependsOn: new string[] { $"provision-{hrmApi.Resource.Name}-containerapp" });

        builder.Pipeline.AddStep($"update-{mcp.Resource.Name}-cors-policy", async context =>
        {
            var deploymentStateManager = context.Services.GetRequiredService<IDeploymentStateManager>();
            var azureDeploymentConfig = await deploymentStateManager.AcquireSectionAsync("Azure");
            var resourceGroupName = azureDeploymentConfig.Data["ResourceGroup"]!.GetValue<string>();

            var configureCorsTask = await context.ReportingStep
                    .CreateTaskAsync($"Configuring CORS policy for {mcp.Resource.Name} ACA resource", context.CancellationToken)
                    .ConfigureAwait(false);

            await using (configureCorsTask.ConfigureAwait(false))
            {
                try
                {
                    await AzCliCommands.EnableContainerAppCorsPolicyForMcpInspector(mcp.Resource.Name, resourceGroupName, context.CancellationToken).ConfigureAwait(false);

                    await configureCorsTask.CompleteAsync(
                        $"Successfully configured CORS policy for {mcp.Resource.Name} ACA resource.",
                        CompletionState.Completed,
                        context.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await configureCorsTask.CompleteAsync(
                        $"Error configuring CORS policy: {ex.Message}",
                        CompletionState.CompletedWithError,
                        context.CancellationToken).ConfigureAwait(false);
                }
            }
        }, requiredBy: WellKnownPipelineSteps.Deploy, dependsOn: new string[] { $"provision-{mcp.Resource.Name}-containerapp" });

        return builder;
    }

    public static IDistributedApplicationBuilder AddCleanAzureResourcesStep(this IDistributedApplicationBuilder builder)
    {
        builder.Pipeline.AddStep("clean-az", async (context) =>
        {
            var confirmAutomatically = Environment.GetCommandLineArgs().Contains("--yes") 
                || Environment.GetCommandLineArgs().Contains("-y");

            if (confirmAutomatically)
            {
                context.Logger.LogWarning("Automatic confirmation enabled; skipping resource deletion prompts.");
            }

            var requireConfirm = !confirmAutomatically;
            var interaction = context.Services.GetRequiredService<IInteractionService>();

            var cleanResourcesTask = await context.ReportingStep
                    .CreateTaskAsync($"Cleaning up Azure-provisioned Aspire resources...", context.CancellationToken)
                    .ConfigureAwait(false);

            await using (cleanResourcesTask.ConfigureAwait(false))
            {
                IReadOnlyList<InteractionInput> confirmInput = [
                    new () {
                        Name = "Confirm?",
                        Required = true,
                        InputType = InputType.Boolean
                    }
                ];

                try
                {
                    var resourceGroupNames = await AzCliCommands.GetAspireResourceGroups(context.CancellationToken).ConfigureAwait(false);

                    if (resourceGroupNames.Length > 0)
                    {
                        foreach (var resourceGroupName in resourceGroupNames)
                        {
                            context.Logger.LogInformation("Deleting resource group {ResourceGroupName}...", resourceGroupName);

                            if (requireConfirm && interaction.IsAvailable)
                            {
                                var shouldDelete = await interaction.PromptInputsAsync(
                                    "Confirm Deletion", $"Deleting resource group {resourceGroupName}",
                                    cancellationToken: context.CancellationToken,
                                    inputs: confirmInput).ConfigureAwait(false);

                                if (shouldDelete.Canceled || bool.Parse(shouldDelete.Data[0].Value ?? "false") is false) {
                                    continue;
                                }
                            }

                            await AzCliCommands.DeleteResourceGroup(resourceGroupName, context.CancellationToken).ConfigureAwait(false);
                        }
                    }

                    var foundryResources = context.Model.Resources
                        .OfType<AzureAIFoundryResource>();

                    foreach (var foundryResource in foundryResources) 
                    {
                        context.Logger.LogInformation("Checking whether {FoundryAccountName} is soft-deleted...", foundryResource.Name);

                        var foundryResourceId = await AzCliCommands.GetSoftDeletedFoundryAccount(foundryResource.Name, context.CancellationToken).ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(foundryResourceId))
                        {
                            if (requireConfirm && interaction.IsAvailable)
                            {
                                var shouldDelete = await interaction.PromptInputsAsync(
                                    "Confirm Deletion", $"Deleting AI Foundry resource: {foundryResourceId}", 
                                    cancellationToken: context.CancellationToken,
                                    inputs: confirmInput).ConfigureAwait(false);

                                if (bool.Parse(shouldDelete.Data?[0].Value ?? "false") is true) {
                                    context.Logger.LogInformation("Purging Foundry account {FoundryAccountName}...", foundryResourceId);

                                    await AzCliCommands.DeleteAzResourceById(foundryResourceId, context.CancellationToken).ConfigureAwait(false);   
                                }
                            }               
                        }
                    }

                    var deploymentStateManager = context.Services.GetRequiredService<IDeploymentStateManager>();
                    var azureDeploymentConfig = await deploymentStateManager.AcquireSectionAsync("Azure");

                    foreach (var azDeploymentState in azureDeploymentConfig.Data)
                    {
                        if (azDeploymentState.Key.StartsWith("Deployments:") || azDeploymentState.Key == "ResourceGroup")
                        {
                            azureDeploymentConfig.Data[azDeploymentState.Key] = null;
                        }
                    }

                    await deploymentStateManager.SaveSectionAsync(azureDeploymentConfig).ConfigureAwait(false);

                    context.Logger.LogInformation("Deleted Azure deployment state");

                    var userSecretsManager = context.Services.GetService<IUserSecretsManager>();

                    if (userSecretsManager?.FilePath != null && File.Exists(userSecretsManager.FilePath))
                    {
                        var secretFileContents = await File.ReadAllTextAsync(
                            userSecretsManager.FilePath,
                            context.CancellationToken).ConfigureAwait(false);

                        JsonNode rootNode = JsonNode.Parse(secretFileContents)!;

                        if (rootNode is JsonObject rootObject)
                        {
                            foreach (var secret in rootObject.DeepClone().AsObject())
                            {
                                if (secret.Key.StartsWith("Azure:Deployments:") || secret.Key == "Azure:ResourceGroup")
                                {
                                    rootObject.Remove(secret.Key);
                                }
                            }

                            var updatedSecretFileContents = rootObject.ToJsonString(
                                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                            await File.WriteAllTextAsync(
                                userSecretsManager.FilePath,
                            updatedSecretFileContents,
                            context.CancellationToken).ConfigureAwait(false);

                            context.Logger.LogInformation("Deleted Azure deployment state from user secrets");
                        }
                    }

                    await cleanResourcesTask.CompleteAsync(
                        $"Successfully cleaned up Aspire resource groups",
                        CompletionState.Completed,
                        context.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await cleanResourcesTask.CompleteAsync(
                        $"Error cleaning up Aspire resource groups: {ex.Message}",
                        CompletionState.CompletedWithError,
                        context.CancellationToken).ConfigureAwait(false);
                }
            }
        });

        return builder;
    }

    public static IDistributedApplicationBuilder AddAzureDemoResources(
        this IDistributedApplicationBuilder builder,
        TokenCredential azureCredential,
        IResourceBuilder<AzureFunctionsProjectResource> mcp,
        IResourceBuilder<AzureStorageResource> hrmDocumentStorage,
        IResourceBuilder<AzureBlobStorageContainerResource> hrmDocumentBlobs)
    {
        var acaEnv = builder.AddAzureContainerAppEnvironment("aca-env");

        var aiSearch = builder.AddAzureSearch("hrm-search-service")
            .WithRunIndexerCommand(azureCredential)
            .ConfigureInfrastructure(infra =>
            {
                var searchService = infra.GetProvisionableResources()
                                    .OfType<SearchService>()
                                    .Single();

                // Keep it affordable for demo purposes
                searchService.SearchSkuName = SearchServiceSkuName.Free;

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

                await RoleAssignments.EnsureRoleAssignmentAsync(
                    azureCredential,
                    storageAccountId!,
                    searchServicePrincipalId,
                    blobDataReaderRole,
                    RoleManagementPrincipalType.ServicePrincipal,
                    cancellationToken);

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

            await RoleAssignments.EnsureRoleAssignmentAsync(
                azureCredential,
                foundryAccountId!,
                searchServicePrincipalId,
                openAiUserRole,
                RoleManagementPrincipalType.ServicePrincipal,
                cancellationToken);

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

                logger.LogInformation("Provisioning AI Search configuration for HRM data...");

                await CreateOrUpdateSearchIndex(azureCredential, logger, cancellationToken);

                logger.LogInformation("Successfully provisioned AI Search index, vectorizer, skillset, and data source for HRM data.");
            });

        builder.Pipeline.AddStep("upload-hrm-pdf-documents", async (context) =>
        {
            var uploadPdfTask = await context.ReportingStep
                    .CreateTaskAsync($"Uploading PDF documents to HRM blob storage", context.CancellationToken)
                    .ConfigureAwait(false);

            await using (uploadPdfTask.ConfigureAwait(false))
            {
                try
                {
                    var signedInUserPrincipalId = await AzCliCommands.GetSignedInUserPrincipalId(context.CancellationToken).ConfigureAwait(false)
                        ?? throw new InvalidOperationException("Failed to determine signed-in user principal ID from Azure CLI");
                    var storageAccountId = await hrmDocumentStorage.GetOutput("StorageAccountResourceId").GetValueAsync(context.CancellationToken)
                        ?? throw new InvalidOperationException("Failed to determine storage account resource ID from HRM document storage resource outputs");

                    await RoleAssignments.EnsureRoleAssignmentAsync(
                        credential: azureCredential,
                        scopeResourceId: storageAccountId,
                        principalId: signedInUserPrincipalId,
                        roleDefinitionGuid: Guid.Parse(StorageBuiltInRole.StorageBlobDataContributor.ToString()),
                        principalType: RoleManagementPrincipalType.User,
                        ct: context.CancellationToken);

                    await hrmDocumentBlobs.Resource.UploadDocumentsToStorageAsync(azureCredential, context.Logger, context.CancellationToken).ConfigureAwait(false);

                    await uploadPdfTask.CompleteAsync(
                        $"Successfully uploaded PDF documents to HRM blob storage.",
                        CompletionState.Completed,
                        context.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await uploadPdfTask.CompleteAsync(
                        $"Error uploading PDF documents: {ex.Message}",
                        CompletionState.CompletedWithError,
                        context.CancellationToken).ConfigureAwait(false);
                }
            }

        }, requiredBy: WellKnownPipelineSteps.Deploy, dependsOn: new string[] { $"provision-{hrmDocumentStorage.Resource.Name}" });

        builder.Pipeline.AddStep($"provision-{aiSearch.Resource.Name}-roles-{foundry.Resource.Name}", async (context) =>
        {
            var assignRolesTask = await context.ReportingStep
                    .CreateTaskAsync($"Assigning RBAC roles to {aiSearch.Resource.Name} for {foundry.Resource.Name}", context.CancellationToken)
                    .ConfigureAwait(false);

            await using (assignRolesTask.ConfigureAwait(false))
            {
                try
                {
                    var searchServicePrincipalIdRaw = await aiSearch.GetOutput("SearchServicePrincipalId").GetValueAsync(context.CancellationToken);
                    var searchServicePrincipalId = Guid.Parse(searchServicePrincipalIdRaw!);

                    var foundryAccountId = await foundry.GetOutput("FoundryAccountResourceId").GetValueAsync(context.CancellationToken);
                    var openAiUserRole = Guid.Parse(CognitiveServicesBuiltInRole.CognitiveServicesOpenAIUser.ToString());
                    await RoleAssignments.EnsureRoleAssignmentAsync(
                        azureCredential,
                        foundryAccountId!,
                        searchServicePrincipalId,
                        openAiUserRole,
                        RoleManagementPrincipalType.ServicePrincipal,
                        context.CancellationToken);

                    await assignRolesTask.CompleteAsync(
                        $"Successfully assigned RBAC roles for HRM Search service.",
                        CompletionState.Completed,
                        context.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await assignRolesTask.CompleteAsync(
                        $"Error assigning RBAC roles: {ex.Message}",
                        CompletionState.CompletedWithError,
                        context.CancellationToken).ConfigureAwait(false);
                }
            }
        }, requiredBy: WellKnownPipelineSteps.Deploy, dependsOn: new string[] { $"provision-{aiSearch.Resource.Name}", $"provision-{foundry.Resource.Name}" });

        builder.Pipeline.AddStep($"provision-{aiSearch.Resource.Name}-roles-{hrmDocumentStorage.Resource.Name}", async (context) =>
        {
            var assignRolesTask = await context.ReportingStep
                    .CreateTaskAsync($"Assigning RBAC roles to {hrmDocumentStorage.Resource.Name} for {aiSearch.Resource.Name}", context.CancellationToken)
                    .ConfigureAwait(false);

            await using (assignRolesTask.ConfigureAwait(false))
            {
                try
                {
                    var searchServicePrincipalIdRaw = await aiSearch.GetOutput("SearchServicePrincipalId").GetValueAsync(context.CancellationToken);
                    var searchServicePrincipalId = Guid.Parse(searchServicePrincipalIdRaw!);

                    var storageAccountId = await hrmDocumentStorage.GetOutput("StorageAccountResourceId").GetValueAsync(context.CancellationToken);
                    var blobDataReaderRole = Guid.Parse(StorageBuiltInRole.StorageBlobDataReader.ToString());
                    await RoleAssignments.EnsureRoleAssignmentAsync(
                        azureCredential,
                        storageAccountId!,
                        searchServicePrincipalId,
                        blobDataReaderRole,
                        RoleManagementPrincipalType.ServicePrincipal,
                        context.CancellationToken);

                    await assignRolesTask.CompleteAsync(
                        $"Successfully assigned RBAC roles for HRM Search service.",
                        CompletionState.Completed,
                        context.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await assignRolesTask.CompleteAsync(
                        $"Error assigning RBAC roles: {ex.Message}",
                        CompletionState.CompletedWithError,
                        context.CancellationToken).ConfigureAwait(false);
                }
            }
        }, requiredBy: WellKnownPipelineSteps.Deploy, dependsOn: new string[] { $"provision-{aiSearch.Resource.Name}", $"provision-{hrmDocumentStorage.Resource.Name}" });

        builder.Pipeline.AddStep($"provision-{aiSearch.Resource.Name}-indexer", async (context) =>
        {
            var provisionHrmSearchIndexerTask = await context.ReportingStep
                    .CreateTaskAsync($"Provisioning AI Search indexer for HRM data", context.CancellationToken)
                    .ConfigureAwait(false);

            await using (provisionHrmSearchIndexerTask.ConfigureAwait(false))
            {
                try
                {
                    await provisionHrmSearchIndexerTask.UpdateStatusAsync("Ensuring search index is created").ConfigureAwait(false);

                    await CreateOrUpdateSearchIndex(
                        azureCredential,
                        context.Logger,
                        context.CancellationToken).ConfigureAwait(false);

                    var searchEndpoint = await aiSearch.Resource.GetConnectionProperty("Uri").GetValueAsync(context.CancellationToken);
                    var indexerClient = new SearchIndexerClient(new Uri(searchEndpoint!), azureCredential);

                    await provisionHrmSearchIndexerTask.UpdateStatusAsync("Indexing HRM PDF document data").ConfigureAwait(false);

                    await HrmSearchSteps.CreateSearchIndexer(
                        indexerClient,
                        $"{aiSearch.Resource.Name}-indexer",
                        $"{aiSearch.Resource.Name}-datasource",
                        $"{aiSearch.Resource.Name}-skillset",
                        $"{aiSearch.Resource.Name}-index",
                        context.CancellationToken).ConfigureAwait(false);

                    await provisionHrmSearchIndexerTask.CompleteAsync(
                        $"Successfully created HRM search indexer for PDF documents.",
                        CompletionState.Completed,
                        context.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await provisionHrmSearchIndexerTask.CompleteAsync(
                        $"Error provisioning AI Search indexer: {ex.Message}",
                        CompletionState.CompletedWithError,
                        context.CancellationToken).ConfigureAwait(false);
                }
            }

        }, requiredBy: WellKnownPipelineSteps.Deploy, dependsOn: new string[] { "upload-hrm-pdf-documents", $"provision-{aiSearch.Resource.Name}" });

        async Task CreateOrUpdateSearchIndex(TokenCredential azureCredential, ILogger logger, CancellationToken cancellationToken)
        {
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
            var skillsetName = $"{aiSearch.Resource.Name}-skillset";
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
        }

        return builder;
    }

    
}