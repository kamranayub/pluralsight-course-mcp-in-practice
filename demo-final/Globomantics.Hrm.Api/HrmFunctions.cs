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

    [OpenApiOperation(operationId: "getAuthenticatedUserIdRaaS", Summary = "Retrieve the Employee ID for the authenticated user.", Description = "Fetches the Employee ID for the authenticated user from Globomantics HRM.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object),
            Description = "A JSON object containing the authenticated user's Employee ID.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized - Invalid or missing Bearer token.")]
    [Function("GetAuthenticatedUserId")]
    public IActionResult GetAuthenticatedUserId([HttpTrigger(AuthorizationLevel.Function, "get", Route = "service/customreport2/tenant/GPT_RAAS")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }

    [OpenApiOperation(operationId: "getEligibleAbsenceTypes", Summary = "Retrieve eligible absence types by Employee ID.", Description = "Fetches a list of eligible absence types for a worker by their Employee ID, with a fixed category filter.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "employeeId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The Employee ID of the worker (passed as `Employee_ID=3050` in the URL).")]
    [OpenApiParameter(name: "category", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "Fixed category filter for the request. This cannot be changed.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object),
            Description = "A JSON array of eligible absence types.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized - Invalid or missing Bearer token.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Worker or absence types not found.")]
    [Function("GetEligibleAbsenceTypes")]
    public IActionResult GetEligibleAbsenceTypes([HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}/eligibleAbsenceTypes")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }

    [OpenApiOperation(operationId: "getWorkerById", Summary = "Retrieve worker details by Employee ID.", Description = "Fetches detailed information of a worker using their Employee ID.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "employeeId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The Employee ID of the worker.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object),
            Description = "A JSON object containing worker details.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized - Invalid or missing Bearer token.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Worker not found.")]
    [Function("GetWorkerById")]
    public IActionResult GetWorkerById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }

    [OpenApiOperation(operationId: "requestTimeOff", Summary = "Request time off for a worker.", Description = "Allows a worker to request time off by providing the necessary details.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "employeeId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The Employee ID of the worker requesting time off.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(object), Required = true, Description = "Time off request details including days, dates, and time off type.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "Time off request created successfully.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid input or missing parameters.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized - Invalid or missing Bearer token.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Worker not found.")]
    [Function("RequestTimeOff")]
    public IActionResult RequestTimeOff([HttpTrigger(AuthorizationLevel.Function, "post", Route = "api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}/requestTimeOff")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }

    [OpenApiOperation(operationId: "getWorkerBenefitPlans", Summary = "Retrieve worker benefit plans enrolled by Employee ID.", Description = "Fetches the benefit plans in which the worker is enrolled using their Employee ID.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "Worker!Employee_ID", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The Employee ID of the worker.")]
    [OpenApiParameter(name: "format", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The format of the response (e.g., `json`).")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object),
            Description = "A JSON array of the worker's enrolled benefit plans.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized - Invalid or missing Bearer token.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Worker or benefit plans not found.")]
    [Function("GetWorkerBenefitPlans")]
    public IActionResult GetWorkerBenefitPlans([HttpTrigger(AuthorizationLevel.Function, "get", Route = "service/customreport2/tenant/GPT_Worker_Benefit_Data")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}