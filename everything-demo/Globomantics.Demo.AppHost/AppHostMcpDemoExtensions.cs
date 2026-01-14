#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES002

using System.Diagnostics;
using System.Runtime.InteropServices;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AppContainers;
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
using Azure.Storage.Blobs;
using Globomantics.Demo.AppHost.Roles;
using Globomantics.Demo.AppHost.Search;
using Globomantics.Demo.AppHost.Storage;
using Microsoft.Extensions.Configuration;
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
        builder.Pipeline.AddStep("update-hrm-api-microsoft-auth", async (context) =>
        {
            var deploymentStateManager = context.Services.GetRequiredService<IDeploymentStateManager>();
            var azureDeploymentConfig = await deploymentStateManager.AcquireSectionAsync("Azure");
            var resourceGroupName = azureDeploymentConfig.Data["ResourceGroup"]!.GetValue<string>();

            var tenantId = await azureTenantId.Resource.GetValueAsync(context.CancellationToken);
            var clientId = await hrmApiAadClientId.Resource.GetValueAsync(context.CancellationToken);
            string[] allowedAudiences = [$"{clientId}", $"api://{clientId}", $"api://{clientId}/user_impersonation"];

            var configureAuthTask = await context.ReportingStep
                    .CreateTaskAsync($"Configuring Microsoft Entra authentication for hrm-api ACA resource", context.CancellationToken)
                    .ConfigureAwait(false);

            await using (configureAuthTask.ConfigureAwait(false))
            {
                try
                {
                    await EnableContainerAppCorsPolicyForMcpInspector(mcp.Resource.Name, resourceGroupName, configureAuthTask, context.CancellationToken).ConfigureAwait(false);
                    await ConfigureContainerAppAuthWithMicrosoft(hrmApi.Resource.Name, resourceGroupName, tenantId!, clientId!, allowedAudiences, configureAuthTask, context.CancellationToken).ConfigureAwait(false);
                    var containerAppEndpoint = await GetContainerAppEndpoint(hrmApi.Resource.Name, resourceGroupName, clientId!, configureAuthTask, context.CancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Failed to retrieve container app endpoint");
                    await ConfigureContainerAppAuthRedirectUri(containerAppEndpoint, clientId!, configureAuthTask, context.CancellationToken).ConfigureAwait(false);

                    await configureAuthTask.CompleteAsync(
                        $"Successfully configured Microsoft Entra authentication for hrm-api ACA resource.",
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
        }, requiredBy: WellKnownPipelineSteps.Deploy, dependsOn: new string[] { "provision-mcp-containerapp", "provision-hrm-api-containerapp" });


        return builder;
    }

    private static async Task EnableContainerAppCorsPolicyForMcpInspector(string containerAppName, string resourceGroupName, IReportingTask configureAuthTask, CancellationToken ct)
    {
        var enableCorsProcess = Process.Start(CreateAzStartInfo(
            "containerapp", "ingress", "cors", "enable",
            "--name", containerAppName,
            "--resource-group", resourceGroupName,
            "--allowed-origins", "http://localhost:6274"
        ));

        if (enableCorsProcess == null)
        {
            await configureAuthTask.CompleteAsync(
                "Failed to start az CLI process",
                CompletionState.CompletedWithError,
                ct).ConfigureAwait(false);
            return;
        }

        var stdoutTask = enableCorsProcess.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = enableCorsProcess.StandardError.ReadToEndAsync(ct);

        await enableCorsProcess.WaitForExitAsync(ct).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (enableCorsProcess.ExitCode != 0)
        {
            await configureAuthTask.CompleteAsync(
                $"az CLI process exited with code {enableCorsProcess.ExitCode}\nSTDOUT: {stdout}\nSTDERR: {stderr}",
                CompletionState.CompletedWithError,
                ct).ConfigureAwait(false);

            return;
        }
    }

    private static async Task ConfigureContainerAppAuthWithMicrosoft(string containerAppName, string resourceGroupName, string tenantId, string clientId, string[] allowedAudiences, IReportingTask configureAuthTask, CancellationToken ct)
    {
        var updateMicrosoftAuthProcess = Process.Start(CreateAzStartInfo(
            "containerapp", "auth", "microsoft", "update",
            "--name", containerAppName,
            "--resource-group", resourceGroupName,
            "--client-id", clientId!,
            "--client-secret-name", "microsoft-provider-authentication-secret", // Matches the environment variable set earlier but as a container app secret
            "--tenant-id", tenantId!,
            "--allowed-audiences", string.Join(",", allowedAudiences),
            "--yes"
        ));

        if (updateMicrosoftAuthProcess == null)
        {
            await configureAuthTask.CompleteAsync(
                "Failed to start az CLI process",
                CompletionState.CompletedWithError,
                ct).ConfigureAwait(false);
            return;
        }

        var stdoutTask = updateMicrosoftAuthProcess.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = updateMicrosoftAuthProcess.StandardError.ReadToEndAsync(ct);

        await updateMicrosoftAuthProcess.WaitForExitAsync(ct).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (updateMicrosoftAuthProcess.ExitCode != 0)
        {
            await configureAuthTask.CompleteAsync(
                $"az CLI process exited with code {updateMicrosoftAuthProcess.ExitCode}\nSTDOUT: {stdout}\nSTDERR: {stderr}",
                CompletionState.CompletedWithError,
                ct).ConfigureAwait(false);

            return;
        }
    }

    private static async Task<Uri?> GetContainerAppEndpoint(string containerAppName, string resourceGroupName, string clientId, IReportingTask configureAuthTask, CancellationToken ct)
    {
        var getContainerAppFqdnProcess = Process.Start(CreateAzStartInfo(
            "containerapp", "show",
            "--name", containerAppName,
            "--resource-group", resourceGroupName,
            "--query", "properties.configuration.ingress.fqdn",
            "--output", "tsv"
        ));

        if (getContainerAppFqdnProcess == null)
        {
            await configureAuthTask.CompleteAsync(
                "Failed to start az CLI process",
                CompletionState.CompletedWithError,
                ct).ConfigureAwait(false);

            return null;
        }

        var stdoutTask = getContainerAppFqdnProcess.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = getContainerAppFqdnProcess.StandardError.ReadToEndAsync(ct);

        await getContainerAppFqdnProcess.WaitForExitAsync(ct).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (getContainerAppFqdnProcess.ExitCode != 0)
        {
            await configureAuthTask.CompleteAsync(
                $"az CLI process exited with code {getContainerAppFqdnProcess.ExitCode}\nSTDOUT: {stdout}\nSTDERR: {stderr}",
                CompletionState.CompletedWithError,
                ct).ConfigureAwait(false);

            return null;
        }

        var fqdn = stdout.Trim();
        return new Uri($"https://{fqdn}");
    }

    private static async Task ConfigureContainerAppAuthRedirectUri(Uri containerEndpoint, string clientId, IReportingTask configureAuthTask, CancellationToken ct)
    {
        var updateEntraAppRedirectUris = Process.Start(CreateAzStartInfo(
            "ad", "app", "update",
            "--id", clientId,
            "--web-redirect-uris", new Uri(containerEndpoint, ".auth/login/aad/callback").ToString()
        ));

        if (updateEntraAppRedirectUris == null)
        {
            await configureAuthTask.CompleteAsync(
                "Failed to start az CLI process",
                CompletionState.CompletedWithWarning,
                ct).ConfigureAwait(false);
            return;
        }

        var stdoutTask = updateEntraAppRedirectUris.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = updateEntraAppRedirectUris.StandardError.ReadToEndAsync(ct);

        await updateEntraAppRedirectUris.WaitForExitAsync(ct).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (updateEntraAppRedirectUris.ExitCode != 0)
        {
            await configureAuthTask.CompleteAsync(
                $"az CLI process exited with code {updateEntraAppRedirectUris.ExitCode}\nSTDOUT: {stdout}\nSTDERR: {stderr}",
                CompletionState.CompletedWithError,
                ct).ConfigureAwait(false);

            return;
        }
    }

    public static IDistributedApplicationBuilder AddAzureMcpDemoResources(
        this IDistributedApplicationBuilder builder,
        TokenCredential azureCredential,
        IResourceBuilder<AzureFunctionsProjectResource> mcp,
        IResourceBuilder<AzureFunctionsProjectResource> hrmApi,
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
                    var signedInUserPrincipalId = await GetSignedInUserPrincipalId(uploadPdfTask, context.CancellationToken).ConfigureAwait(false) 
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

        builder.Pipeline.AddStep($"rbac-{aiSearch.Resource.Name}", async (context) =>
        {
            var assignRolesTask = await context.ReportingStep
                    .CreateTaskAsync($"Assigning RBAC roles for HRM Search service", context.CancellationToken)
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
        }, requiredBy: WellKnownPipelineSteps.Deploy, dependsOn: new string[] { $"provision-{aiSearch.Resource.Name}", $"provision-{foundry.Resource.Name}" });

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

    private static async Task<Guid?> GetSignedInUserPrincipalId(IReportingTask configureAuthTask, CancellationToken ct)
    {
        var getSignedInUserIdProcess = Process.Start(CreateAzStartInfo(
            "ad", "signed-in-user", "show",
            "--query", "id",
            "--output", "tsv"
        ));

        if (getSignedInUserIdProcess == null)
        {
            await configureAuthTask.CompleteAsync(
                "Failed to start az CLI process",
                CompletionState.CompletedWithError,
                ct).ConfigureAwait(false);

            return null;
        }

        var stdoutTask = getSignedInUserIdProcess.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = getSignedInUserIdProcess.StandardError.ReadToEndAsync(ct);

        await getSignedInUserIdProcess.WaitForExitAsync(ct).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (getSignedInUserIdProcess.ExitCode != 0)
        {
            await configureAuthTask.CompleteAsync(
                $"az CLI process exited with code {getSignedInUserIdProcess.ExitCode}\nSTDOUT: {stdout}\nSTDERR: {stderr}",
                CompletionState.CompletedWithError,
                ct).ConfigureAwait(false);

            return null;
        }

        return Guid.TryParse(stdout.Trim(), out var userId) ? userId : null;
    }

    static ProcessStartInfo CreateAzStartInfo(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "az",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi.ArgumentList.Add("/d");
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add("az.cmd");
        }

        foreach (var a in args)
            psi.ArgumentList.Add(a);

        return psi;
    }
}