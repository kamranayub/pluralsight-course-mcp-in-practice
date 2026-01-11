using Aspire.Hosting.Azure;
using Azure.Core;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Globomantics.Demo.AppHost.Search;

internal static class HrmSearchServiceResourceExtensions
{
    public static IResourceBuilder<AzureSearchResource> WithRunIndexerCommand(this IResourceBuilder<AzureSearchResource> builder, TokenCredential credential)
    {
        var commandOptions = new CommandOptions
        {
            UpdateState = OnUpdateResourceState,
            IconName = "TaskListSquareSparkle",
            IconVariant = IconVariant.Filled,
            Description = "Run the AI Search indexer to index HRM documents.",
            IsHighlighted = true
        };

        builder.WithCommand(
            name: "run-indexer",
            displayName: "Run Search Indexer",
            executeCommand: context => OnRunIndexerCommandAsync(builder, context, credential),
            commandOptions: commandOptions);

        return builder;
    }

    private static async Task<ExecuteCommandResult> OnRunIndexerCommandAsync(
        IResourceBuilder<AzureSearchResource> builder,
        ExecuteCommandContext context,
        TokenCredential azureCredential)
    {
        var dataSourceName = $"{builder.Resource.Name}-datasource";
        var skillsetName =$"{builder.Resource.Name}-skillset";
        var indexName =$"{builder.Resource.Name}-index";
        var indexerName = $"{builder.Resource.Name}-indexer";
        var searchEndpoint = await builder.Resource.GetConnectionProperty("Uri").GetValueAsync(context.CancellationToken);
        var indexerClient = new SearchIndexerClient(new Uri(searchEndpoint!), azureCredential);

        await HrmSearchSteps.CreateSearchIndexer(
            indexerClient: indexerClient,
            indexerName: indexerName,
            dataSource: dataSourceName,
            skillSet: skillsetName,
            index: indexName,
            cancellationToken: context.CancellationToken
        );

        return CommandResults.Success();
    }

    private static ResourceCommandState OnUpdateResourceState(
        UpdateCommandStateContext context)
    {
        var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Updating resource state: {ResourceSnapshot}",
                context.ResourceSnapshot);
        }

        return context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
            ? ResourceCommandState.Enabled
            : ResourceCommandState.Disabled;
    }
}