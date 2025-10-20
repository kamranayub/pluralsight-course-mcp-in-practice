using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server;

public static class Filters
{
     public static McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> ListPromptsFilter(McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> next)
    {
        return async (request, cancellationToken) =>
        {
            var result = await next(request, cancellationToken);
            
            // Detect whether client is Claude Desktop and filter out smart prompts
            // This is just an example; in real scenarios, more robust detection may be needed            
            var clientName = request.Server.ClientInfo?.Name;
            if (clientName != null && clientName.Contains("Claude", StringComparison.OrdinalIgnoreCase))
            {
                result.Prompts = result.Prompts.Where(p => p.Title?.Contains("Smart", StringComparison.OrdinalIgnoreCase) == false).ToList();
            }

            return result;
        };
    }
}