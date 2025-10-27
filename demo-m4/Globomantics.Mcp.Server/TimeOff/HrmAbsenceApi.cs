using Globomantics.Hrm.Api;
using RestEase;

namespace Globomantics.Mcp.Server.TimeOff;

public interface IHrmAbsenceApi
{
    [Get("api/service/customreport2/tenant/GPT_RAAS")]
    Task<EmployeeIdResponse> GetAuthenticatedUserIdAsync(CancellationToken cancellationToken);

    [Get("api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}/eligibleAbsenceTypes")]
    Task<AbsenceTypesResponse> GetEligibleAbsenceTypesAsync([Path] string employeeId, [Query] string category, CancellationToken cancellationToken);

    [Get("api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}")]
    Task<Worker> GetWorkerByIdAsync([Path] string employeeId, CancellationToken cancellationToken);

    [Post("api/absenceManagement/v1/tenant/workers/Employee_ID={employeeId}/requestTimeOff")]
    Task<TimeOffResponse> RequestTimeOffAsync([Path] string employeeId, [Body] TimeOffRequest request, CancellationToken cancellationToken);

    [Get("api/service/customreport2/tenant/GPT_Worker_Benefit_Data")]
    Task<BenefitPlansResponse> GetWorkerBenefitPlansAsync([Query(Name = "Worker!Employee_ID")] string employeeId, [Query] string format, CancellationToken cancellationToken);

    [Get("api/service/customreport2/tenant/GPT_Worker_Planned_Time_Off")]
    Task<PlannedTimeOffResponse> GetWorkerPlannedTimeOffAsync([Query(Name = "Worker!Employee_ID")] string employeeId, [Query] string format, CancellationToken cancellationToken);
}
