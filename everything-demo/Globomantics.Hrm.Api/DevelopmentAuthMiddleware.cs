using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

/// <summary>
/// Middleware to stamp a response header on the result of http trigger invocation.
/// </summary>
internal sealed class DevelopmentAuthMiddleware : IFunctionsWorkerMiddleware
{
    public static AzureCliCredential DevelopmentCredential = new();

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();
        var token = await DevelopmentCredential.GetTokenAsync(
            new TokenRequestContext([$"https://management.core.windows.net"]), context.CancellationToken);

        var clientPrincipalJson = ClaimsPrincipalParser.ToClientPrincipalJson(token);

        context.GetLogger<DevelopmentAuthMiddleware>()
            .LogDebug("Adding Default Azure Identity auth principal to request header x-ms-client-principal: {Principal}", clientPrincipalJson);

        requestData?.Headers.Add("x-ms-client-principal", Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(
                clientPrincipalJson)));

        await next(context);

    }
}