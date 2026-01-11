using Azure.Search.Documents.Indexes;
using System.Text.Json.Serialization;

namespace Globomantics.Demo.AppHost.Search;

public class HrmDocumentIndex
{
    public const string VectorProfileName = "hrm-documents-profile";

    [SearchableField(IsSortable = true, IsKey = true, AnalyzerName = "keyword")]
    [JsonPropertyName("chunk_id")]
    public required string ChunkId { get; set; }

    [SearchableField(IsFilterable = true)]
    [JsonPropertyName("parent_id")]
    public required string ParentId { get; set; }

    [SearchableField]
    [JsonPropertyName("chunk")]
    public required string Chunk { get; set; }

    [SearchableField]
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [VectorSearchField(VectorSearchDimensions = 1536, VectorSearchProfileName = VectorProfileName)]
    [JsonPropertyName("text_vector")]
    public required IReadOnlyList<float> TextVector { get; set; }
}