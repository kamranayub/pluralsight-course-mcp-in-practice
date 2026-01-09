using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Middleware to stamp a response header on the result of http trigger invocation.
/// </summary>
internal sealed class DevelopmentAuthMiddleware : IFunctionsWorkerMiddleware
{
    public static DefaultAzureCredential DevelopmentCredential = new();

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var config = context.InstanceServices.GetRequiredService<IConfiguration>();
        var hrmAppId = config["HRM_API_AAD_CLIENT_ID"];
        var requestData = await context.GetHttpRequestDataAsync();
        var token = await DevelopmentCredential.GetTokenAsync(new Azure.Core.TokenRequestContext([$"api://{hrmAppId}/user_impersonation"]), default);

        var clientPrincipalJson = ClaimsPrincipalParser.ToClientPrincipalJson(token);

        context.GetLogger<DevelopmentAuthMiddleware>()
            .LogDebug("Adding Default Azure Identity auth principal to request header x-ms-client-principal: {Principal}", clientPrincipalJson);

        requestData?.Headers.Add("x-ms-client-principal", Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(
                clientPrincipalJson)));

        await next(context);

    }
}