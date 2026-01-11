namespace Globomantics.Hrm.Api;

public record Worker(
    string Id,
    WorkerName Name,
    string HQLocation,
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

public record BenefitPlanType(
    string Id,
    string Name
);

public record BenefitPlan(
    string PlanName,
    BenefitPlanType PlanType,
    string Coverage,
    DateTime StartDate,
    DateTime EndDate
);

public record TimeOffRequest(
    List<TimeOffDay> Days
);

public record TimeOffDay(
    string Start,
    string Date,
    string End,
    double DailyQuantity,
    AbsenceType TimeOffType
);

public record PlannedTimeOff(
    List<TimeOffDay> PlannedDays
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

public record PlannedTimeOffResponse(
    PlannedTimeOff PlannedTimeOff
);

public record TimeOffResponse(
    bool Success,
    string Message,
    string RequestId
);