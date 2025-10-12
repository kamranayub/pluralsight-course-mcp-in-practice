using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Globomantics.Hrm.Api;

public class HrmFunctions
{
    private readonly ILogger<HrmFunctions> _logger;

    public HrmFunctions(ILogger<HrmFunctions> logger)
    {
        _logger = logger;
    }

    [OpenApiOperation(operationId: "GetAuthenticatedUserId", Summary = "Get the authenticated user ID.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string),
            Description = "The OK response message containing a JSON result.")]
    [Function("GetAuthenticatedUserId")]
    public IActionResult GetAuthenticatedUserId([HttpTrigger(AuthorizationLevel.Function, "get", Route = "service/customreport2/tenant/GPT_RAAS")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }

    [OpenApiOperation(operationId: "GetEligibleAbsenceTypes", Summary = "Get eligible absence types for the authenticated user.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string),
            Description = "The OK response message containing a JSON result.")]
    [Function("GetEligibleAbsenceTypes")]
    public IActionResult GetEligibleAbsenceTypes([HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}/eligibleAbsenceTypes")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }

    [OpenApiOperation(operationId: "GetWorkerById", Summary = "Get worker details by worker ID.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string),
            Description = "The OK response message containing a JSON result.")]
    [Function("GetWorkerById")]
    public IActionResult GetWorkerById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }

    [OpenApiOperation(operationId: "RequestTimeOff", Summary = "Request time off for the authenticated user.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string),
            Description = "The OK response message containing a JSON result.")]
    [Function("RequestTimeOff")]
    public IActionResult RequestTimeOff([HttpTrigger(AuthorizationLevel.Function, "post", Route = "api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}/requestTimeOff")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }

    [OpenApiOperation(operationId: "GetWorkerBenefitPlans", Summary = "Retrieve worker benefit plans enrolled by Employee ID.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string),
            Description = "The OK response message containing a JSON result.")]
    [Function("GetWorkerBenefitPlans")]
    public IActionResult GetWorkerBenefitPlans([HttpTrigger(AuthorizationLevel.Function, "get", Route = "service/customreport2/tenant/GPT_Worker_Benefit_Data")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}