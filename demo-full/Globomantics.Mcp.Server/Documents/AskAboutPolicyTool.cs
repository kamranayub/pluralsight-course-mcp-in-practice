using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Documents;

[McpServerToolType]
public class AskAboutPolicyTool(SearchClient searchClient)
{
    private readonly SearchClient searchClient = searchClient;

    [McpServerTool(Title = "Ask About Policy", OpenWorld = true), Description(
        """
        Help Globomantics employees answer questions about benefits policies such as medical, dental, vision, and retirement plans.
        The Plan Time Off tool is better for answering questions about sabbaticals, leave, and vacation.
        This tool returns relevant excerpts from the benefit policy documents to inform your response.
        If the tool result is not supported or is missing specific policy information, NEVER try to assume and inform the user that you cannot provide an answer,
        and that they may need to contact HR for further assistance.
        """)]
    public async Task<IEnumerable<ContentBlock>> AskAboutPolicy(
        [Description("The relevant policy type the user is asking about")] PolicyQuestionType policyQuestionType,
        CancellationToken cancellationToken)
    {
        var contentBlocks = new List<ContentBlock>
        {
            new TextContentBlock
            {
                Text = $"Here are some relevant time off policy document excerpts to help answer:"
            }
        };

        var question = policyQuestionType switch
        {
            PolicyQuestionType.VacationOrHolidays => "vacation or holidays",
            PolicyQuestionType.SickLeave => "sick leave",
            PolicyQuestionType.FamilyLeave => "FMLA",
            PolicyQuestionType.PersonalLeaveOfAbsence => "personal leave of absence",
            PolicyQuestionType.Sabbatical => "sabbatical",
            PolicyQuestionType.MedicalBenefits => "medical benefits",
            PolicyQuestionType.VisionBenefits => "vision benefits",
            PolicyQuestionType.DentalBenefits => "dental benefits",
            PolicyQuestionType.RetirementPlans => "retirement plans",
            _ => throw new ArgumentOutOfRangeException(nameof(policyQuestionType), policyQuestionType, null)
        };

        await foreach (var excerptBlock in ProvideRelevantPlanExcerptsAsync(question, 1, cancellationToken))
        {
            contentBlocks.Add(excerptBlock);
        }

        return contentBlocks;
    }

    private async IAsyncEnumerable<ContentBlock> ProvideRelevantPlanExcerptsAsync(string queryText, int k, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var searchOptions = new SearchOptions
        {
            Filter = "",
            Size = k,
            Select = { "title", "chunk_id", "chunk" },
            IncludeTotalCount = true,
            VectorSearch = new()
            {
                Queries =
                {
                    new VectorizableTextQuery(text: queryText)
                    {
                        KNearestNeighborsCount = k,
                        Fields = {"text_vector"},
                        Exhaustive = false
                    }
                }
            }
        };

        var searchResults = await searchClient.SearchAsync<SearchDocument>(null, searchOptions, cancellationToken: cancellationToken);

        if (searchResults.HasValue)
        {
            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                yield return new TextContentBlock
                {
                    Text = $"Document ID: {result.Document["title"]}\n---\n{result.Document["chunk"]}"
                };
            }
        }
    }
}

public enum PolicyQuestionType
{
    VacationOrHolidays,
    SickLeave,
    FamilyLeave,
    PersonalLeaveOfAbsence,
    Sabbatical,
    MedicalBenefits,
    VisionBenefits,
    DentalBenefits,
    RetirementPlans
}