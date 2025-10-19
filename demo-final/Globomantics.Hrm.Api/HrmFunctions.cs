using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
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
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object),
            Description = "A JSON object containing the authenticated user's Employee ID.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized - Invalid or missing Bearer token.")]
    [Function("GetAuthenticatedUserId")]
    public async Task<HttpResponseData> GetAuthenticatedUserId([HttpTrigger(AuthorizationLevel.Function, "get", Route = "service/customreport2/tenant/GPT_RAAS")] HttpRequestData req)
    {
        _logger.LogInformation("Getting authenticated user ID");

        var userEmail = GetUserEmailFromToken(req);

        if (string.IsNullOrEmpty(userEmail) || !MockDataStore.UserToEmployeeId.ContainsKey(userEmail))
        {
            return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid or missing authentication");
        }

        var employeeId = MockDataStore.UserToEmployeeId[userEmail];
        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(new EmployeeIdResponse(employeeId));

        return response;
    }

    [OpenApiOperation(operationId: "getEligibleAbsenceTypes", Summary = "Retrieve eligible absence types by Employee ID.", Description = "Fetches a list of eligible absence types for a worker by their Employee ID, with a fixed category filter.")]
    [OpenApiParameter(name: "employeeId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The Employee ID of the worker (passed as `Employee_ID=3050` in the URL).")]
    [OpenApiParameter(name: "category", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "Fixed category filter for the request. This cannot be changed.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object),
            Description = "A JSON array of eligible absence types.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized - Invalid or missing Bearer token.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Worker or absence types not found.")]
    [Function("GetEligibleAbsenceTypes")]
    public async Task<HttpResponseData> GetEligibleAbsenceTypes([HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}/eligibleAbsenceTypes")] HttpRequestData req, string employeeId)
    {
        _logger.LogInformation($"Getting eligible absence types for employee {employeeId}");

        if (!MockDataStore.Workers.ContainsKey(employeeId))
        {
            return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Worker not found");
        }

        // Check for category query parameter (required per spec)
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var category = query["category"];

        if (string.IsNullOrEmpty(category))
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Category parameter required");
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            new AbsenceTypesResponse(MockDataStore.AbsenceTypes));
        return response;
    }

    [OpenApiOperation(operationId: "getWorkerById", Summary = "Retrieve worker details by Employee ID.", Description = "Fetches detailed information of a worker using their Employee ID.")]
    [OpenApiParameter(name: "employeeId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The Employee ID of the worker.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object),
            Description = "A JSON object containing worker details.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized - Invalid or missing Bearer token.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Worker not found.")]
    [Function("GetWorkerById")]
    public async Task<HttpResponseData> GetWorkerById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}")] HttpRequestData req, string employeeId)
    {
        _logger.LogInformation($"Getting worker details for employee {employeeId}");

        if (!MockDataStore.Workers.TryGetValue(employeeId, out var worker))
        {
            return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Worker not found");
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(worker);
        return response;
    }

    [OpenApiOperation(operationId: "requestTimeOff", Summary = "Request time off for a worker.", Description = "Allows a worker to request time off by providing the necessary details.")]
    [OpenApiParameter(name: "employeeId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The Employee ID of the worker requesting time off.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(object), Required = true, Description = "Time off request details including days, dates, and time off type.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "Time off request created successfully.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid input or missing parameters.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized - Invalid or missing Bearer token.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Worker not found.")]
    [Function("RequestTimeOff")]
    public async Task<HttpResponseData> RequestTimeOff([HttpTrigger(AuthorizationLevel.Function, "post", Route = "api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}/requestTimeOff")] HttpRequestData req, string employeeId)
    {
        _logger.LogInformation($"Requesting time off for employee {employeeId}");

        if (!MockDataStore.Workers.ContainsKey(employeeId))
        {
            return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Worker not found");
        }

        TimeOffRequest? timeOffRequest;
        try
        {
            timeOffRequest = await req.ReadFromJsonAsync<TimeOffRequest>();
            if (timeOffRequest == null || timeOffRequest.Days == null || !timeOffRequest.Days.Any())
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }
        }
        catch
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON format");
        }

        // Validate time off type IDs
        var validTimeOffTypeIds = MockDataStore.AbsenceTypes.Select(a => a.Id).ToHashSet();
        foreach (var day in timeOffRequest.Days)
        {
            if (!validTimeOffTypeIds.Contains(day.TimeOffType.Id))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, 
                    $"Invalid time off type ID: {day.TimeOffType.Id}");
            }
        }

        // Store the request
        MockDataStore.TimeOffRequests.Add(timeOffRequest);
        var requestId = Guid.NewGuid().ToString();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            new TimeOffResponse(true, "Time off request created successfully", requestId));
        return response;
    }

    [OpenApiOperation(operationId: "getWorkerBenefitPlans", Summary = "Retrieve worker benefit plans enrolled by Employee ID.", Description = "Fetches the benefit plans in which the worker is enrolled using their Employee ID.")]
    [OpenApiParameter(name: "Worker!Employee_ID", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The Employee ID of the worker.")]
    [OpenApiParameter(name: "format", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The format of the response (e.g., `json`).")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object),
            Description = "A JSON array of the worker's enrolled benefit plans.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized - Invalid or missing Bearer token.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Worker or benefit plans not found.")]
    [Function("GetWorkerBenefitPlans")]
    public async Task<HttpResponseData> GetWorkerBenefitPlans([HttpTrigger(AuthorizationLevel.Function, "get", Route = "service/customreport2/tenant/GPT_Worker_Benefit_Data")] HttpRequestData req)
    {
        _logger.LogInformation("Getting worker benefit plans");

        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var employeeId = query["Worker!Employee_ID"];
        var format = query["format"];

        if (string.IsNullOrEmpty(employeeId))
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Employee ID required");
        }

        if (format != "json")
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Only JSON format supported");
        }

        if (!MockDataStore.BenefitPlans.TryGetValue(employeeId, out var plans))
        {
            return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Benefit plans not found");
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new BenefitPlansResponse(plans));
        return response;
    }

    private string GetUserEmailFromToken(HttpRequestData req)
    {
        var claims = req.Identities.SelectMany(i => i.Claims).ToList();
        return claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "user@globomantics.com"; // Default for no authentication
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}