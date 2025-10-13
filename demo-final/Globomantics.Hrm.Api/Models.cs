namespace Globomantics.Hrm.Api;

public record Worker(
    string Id,
    WorkerName Name,
    string Position,
    string Email
);

public record WorkerName(
    string FirstName,
    string LastName
);

public record AbsenceType(
    string Id,
    string Name
);

public record BenefitPlan(
    string PlanName,
    string Coverage,
    string StartDate,
    string EndDate
);

public record TimeOffRequest(
    List<TimeOffDay> Days
);

public record TimeOffDay(
    string Start,
    string Date,
    string End,
    double DailyQuantity,
    TimeOffType TimeOffType
);

public record TimeOffType(
    string Id
);

public record EmployeeIdResponse(
    string EmployeeId
);

public record AbsenceTypesResponse(
    List<AbsenceType> AbsenceTypes
);

public record BenefitPlansResponse(
    List<BenefitPlan> BenefitPlans
);

public record TimeOffResponse(
    bool Success,
    string Message,
    string RequestId
);