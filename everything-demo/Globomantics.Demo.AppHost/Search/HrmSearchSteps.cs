using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Logging;

namespace Globomantics.Demo.AppHost.Search;

internal static class HrmSearchSteps
{
    internal static async Task<SearchIndexerDataSourceConnection> CreateOrUpdateSearchDataSource(ILogger logger, SearchIndexerClient indexerClient, string dataSourceName, string blobContainerName, string blobConnectionString)
    {
        var dataSource = new SearchIndexerDataSourceConnection(
            name: dataSourceName,
            type: SearchIndexerDataSourceType.AzureBlob,
            connectionString: blobConnectionString,
            container: new SearchIndexerDataContainer(blobContainerName))
        {
            Description = "Connection to Globomantics HRM document blobs"
        };

        await indexerClient.CreateOrUpdateDataSourceConnectionAsync(dataSource);

        logger.LogDebug("Created HRM document blob data source for AI Search.");

        return dataSource;
    }

    internal static SplitSkill CreateSplitSkill()
    {
        List<InputFieldMappingEntry> inputMappings =
        [
            new InputFieldMappingEntry("text")
        {
            Source = "/document/content"
        }
        ];

        List<OutputFieldMappingEntry> outputMappings =
        [
            new OutputFieldMappingEntry("textItems")
        {
            TargetName = "pages",
        },
    ];

        SplitSkill splitSkill = new(inputMappings, outputMappings)
        {
            Description = "Split skill to chunk documents",
            Context = "/document",
            TextSplitMode = TextSplitMode.Pages,
            MaximumPageLength = 2000,
            PageOverlapLength = 500,
            MaximumPagesToTake = 0,
            Unit = SplitSkillUnit.Characters,
            DefaultLanguageCode = SplitSkillLanguage.En
        };

        return splitSkill;
    }

    internal static AzureOpenAIEmbeddingSkill CreateEmbeddingSkill(string deploymentId, string modelName, Uri embeddingResourceUri)
    {
        List<InputFieldMappingEntry> inputMappings =
        [
            new InputFieldMappingEntry("text")
        {
            Source = "/document/pages/*"
        }
        ];

        List<OutputFieldMappingEntry> outputMappings =
        [
            new OutputFieldMappingEntry("embedding")
        {
            TargetName = "text_vector",
        },
    ];

        var embeddingSkill = new AzureOpenAIEmbeddingSkill(inputMappings, outputMappings)
        {
            Description = "Generate embeddings for document pages",
            Context = "/document/pages/*",
            DeploymentName = deploymentId,
            ResourceUri = embeddingResourceUri,
            ModelName = modelName,
            Dimensions = 1536
        };

        return embeddingSkill;
    }

    internal static async Task<SearchIndex> CreateOrUpdateSearchIndex(ILogger logger, SearchIndexClient indexClient, string indexName, string embeddingDeploymentName, string embeddingModelName, Uri embeddingResourceUri)
    {
        var vectorSearchHnswConfig = "hrm-documents-vector-config";

        FieldBuilder builder = new();
        var index = new SearchIndex(indexName)
        {
            Fields = builder.Build(typeof(HrmDocumentIndex)),
            VectorSearch = new()
            {
                Profiles =
            {
                new VectorSearchProfile(HrmDocumentIndex.VectorProfileName, vectorSearchHnswConfig)
                {
                    VectorizerName = "openai"
                }
            },
                Algorithms =
            {
                new HnswAlgorithmConfiguration(vectorSearchHnswConfig)
            },
                Vectorizers =
            {
                new AzureOpenAIVectorizer("openai")
                {
                    Parameters = new AzureOpenAIVectorizerParameters()
                    {
                        DeploymentName = embeddingDeploymentName,
                        ResourceUri = embeddingResourceUri,
                        ModelName = embeddingModelName
                    }
                }
            }
            }
        };

        try
        {
            indexClient.GetIndex(index.Name);
            indexClient.DeleteIndex(index.Name);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            //if the specified index not exist, 404 will be thrown.
        }

        await indexClient.CreateIndexAsync(index);

        logger.LogDebug("Created HRM document search index for AI Search.");

        return index;
    }

    internal static async Task<SearchIndexerSkillset> CreateOrUpdateSearchSkillSet(ILogger logger, SearchIndexerClient indexerClient, string skillsetName, IList<SearchIndexerSkill> skills, string indexName)
    {
        var mappings = new List<InputFieldMappingEntry>()
    {
        new("text_vector")
        {
            Source = "/document/pages/*/text_vector"
        },
        new("chunk")
        {
            Source = "/document/pages/*"
        },
        new ("title")
        {
            Source = "/document/title"
        }
    };

        var selectors = new List<SearchIndexerIndexProjectionSelector>()
    {
        new(
            indexName,
            parentKeyFieldName: "parent_id",
            sourceContext: "/document/pages/*",
            mappings)
    };

        var skillset = new SearchIndexerSkillset(skillsetName, skills)
        {
            Description = "Globomantics HRM document skillset",
            CognitiveServicesAccount = new DefaultCognitiveServicesAccount(),
            IndexProjection = new SearchIndexerIndexProjection(selectors)
            {
                Parameters = new SearchIndexerIndexProjectionsParameters()
                {
                    ProjectionMode = IndexProjectionMode.SkipIndexingParentDocuments
                }
            },
        };

        // Create the skillset in your search service.
        // The skillset does not need to be deleted if it was already created
        // since we are using the CreateOrUpdate method
        await indexerClient.CreateOrUpdateSkillsetAsync(skillset);

        logger.LogDebug("Created HRM document skillset for AI Search.");

        return skillset;
    }

    internal static async Task<SearchIndexer> CreateSearchIndexer(SearchIndexerClient indexerClient, string indexerName, string dataSource, string skillSet, string index, CancellationToken cancellationToken)
    {
        IndexingParameters indexingParameters = new()
        {
            MaxFailedItems = -1,
            MaxFailedItemsPerBatch = -1,
        };
        indexingParameters.Configuration.Add("parsingMode", "default");

        SearchIndexer indexer = new (indexerName, dataSource, index)
        {
            Description = "Indexer for Globomantics HRM documents",
            SkillsetName = skillSet,
            Parameters = indexingParameters
        };

        indexer.FieldMappings.Add(new FieldMapping("metadata_storage_name")
        {
            TargetFieldName = "title"

        });

        try
        {
            await indexerClient.GetIndexerAsync(indexer.Name, cancellationToken);
            await indexerClient.DeleteIndexerAsync(indexer.Name, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            //if the specified indexer not exist, 404 will be thrown.
        }

        
        await indexerClient.CreateIndexerAsync(indexer, cancellationToken);
        
        return indexer;
    }
}