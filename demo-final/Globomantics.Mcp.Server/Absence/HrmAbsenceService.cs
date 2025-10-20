using Azure.Core;
using RestEase;

namespace Globomantics.Mcp.Server.Absence;

public interface IHrmAbsenceApi
{
    // RestEase interface for HRM Absence Service
    [Get("api/service/customreport2/tenant/GPT_RAAS")]
    Task<HrmAbsenceReportResponse> GetAbsenceReportAsync();

    [Get("api/service/customreport2/tenant/GPT_RAAS")]
    Task<EmployeeIdResponse> GetAuthenticatedUserIdAsync();

    [Get("api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}/eligibleAbsenceTypes")]
    Task<AbsenceTypesResponse> GetEligibleAbsenceTypesAsync([Path] string employeeId, [Query] string category);

    [Get("api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}")]
    Task<Worker> GetWorkerByIdAsync([Path] string employeeId);

    [Post("api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}/requestTimeOff")]
    Task<TimeOffResponse> RequestTimeOffAsync([Path] string employeeId, [Body] TimeOffRequest request);

    // Models (kept here for convenience; can be moved to dedicated files)
    public class HrmAbsenceReportResponse
    {
        public string EmployeeId { get; set; } = default!;
    }

    public class EmployeeIdResponse
    {
        public string EmployeeId { get; set; } = default!;
    }

    public class AbsenceType
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
    }

    public class AbsenceTypesResponse
    {
        public System.Collections.Generic.List<AbsenceType> AbsenceTypes { get; set; } = new();
    }

    public class Worker
    {
        public string EmployeeId { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string Email { get; set; } = default!;
    }

    public class TimeOffRequest
    {
        public System.Collections.Generic.List<TimeOffDay> Days { get; set; } = new();
    }

    public class TimeOffDay
    {
        public System.DateTime Date { get; set; }
        public TimeOffType TimeOffType { get; set; } = new();
    }

    public class TimeOffType
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
    }

    public class TimeOffResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = default!;
        public string RequestId { get; set; } = default!;

        public TimeOffResponse() { }

        public TimeOffResponse(bool success, string message, string requestId)
        {
            Success = success;
            Message = message;
            RequestId = requestId;
        }
    }

    public class BenefitPlan
    {
        public string PlanId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string EnrollmentStatus { get; set; } = default!;
    }

    public class BenefitPlansResponse
    {
        public System.Collections.Generic.List<BenefitPlan> Plans { get; set; } = new();
    }
}
