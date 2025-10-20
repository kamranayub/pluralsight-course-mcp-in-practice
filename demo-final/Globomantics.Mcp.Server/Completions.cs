using Globomantics.Mcp.Server.Documents;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server;

public static class Completions
{
    public static ValueTask<CompleteResult> CompleteHandler(RequestContext<CompleteRequestParams> requestContext, CancellationToken cancellationToken)
    {
        if (requestContext.Params?.Ref is ResourceTemplateReference resourceTemplateRef)
        {
            if (resourceTemplateRef.Uri == "globomantics://hrm/benefit-documents/{documentId}")
            {
                return DocumentResources.GetCompletionsForDocumentRequest(requestContext, cancellationToken);
            }
        }

        return ValueTask.FromResult(new CompleteResult()
        {
            Completion = new Completion()
        });
    }

}