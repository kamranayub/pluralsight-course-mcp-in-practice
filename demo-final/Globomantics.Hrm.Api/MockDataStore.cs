namespace Globomantics.Hrm.Api;

public static class MockDataStore
{
    // Map email to employee ID (simulating authenticated user lookup)
    public static readonly Dictionary<string, string> UserToEmployeeId = new()
    {
        { "psazureuser@kamranayub.com", "5050" },
        { "subkamran@hotmail.com", "5020" },
        { "user@globomantics.com", "3050" }
    };

    // Worker data
    public static readonly Dictionary<string, Worker> Workers = new()
    {
        {
            "5050",
            new Worker(
                "5050",
                new WorkerName("PS", "User"),
                "Software Engineer",
                "employee@globomantics.com"
            )
        },
        {
            "5020",
            new Worker(
                "5020",
                new WorkerName("Kamran", "Ayub"),
                "CEO",
                "ceo@globomantics.com"
            )
        },
        {
            "3050",
            new Worker(
                "3050",
                new WorkerName("Bob", "Johnson"),
                "Senior Developer",
                "bob.johnson@globomantics.com"
            )
        }
    };

    // Absence types (time off types)
    public static readonly List<AbsenceType> AbsenceTypes = new()
    {
        new AbsenceType("b35340ce4321102030f8b5a848bc0000", "Flexible Time Off"),
        new AbsenceType("21bd0afbfbf21011e6ccc4dc170e0000", "Sick Leave"),
        new AbsenceType("a1234567890abcdef1234567890abcde", "Vacation"),
        new AbsenceType("b9876543210fedcba0987654321fedcb", "Personal Day")
    };

    // Benefit plan types
    public static readonly List<BenefitPlanType> BenefitPlanTypes = new()
    {
        new BenefitPlanType("Medical", "Medical Insurance"),
        new BenefitPlanType("Dental", "Dental Insurance"),
        new BenefitPlanType("Vision", "Vision Insurance"),
        new BenefitPlanType("Retirement", "401k Retirement Plan")
    };

    // Benefit plans per employee
    public static readonly Dictionary<string, List<BenefitPlan>> BenefitPlans = new()
    {
        {
            "5050",
            new List<BenefitPlan>
            {
                new BenefitPlan("Health Insurance Premium", BenefitPlanTypes[0], "Employee + Family", new DateTime(DateTime.Now.Year, 1, 1), new DateTime(DateTime.Now.Year, 12, 31)),
                new BenefitPlan("Dental Insurance", BenefitPlanTypes[1], "Employee Only", new DateTime(DateTime.Now.Year, 1, 1), new DateTime(DateTime.Now.Year, 12, 31)),
                new BenefitPlan("401k Plan", BenefitPlanTypes[3], "6% Match", new DateTime(DateTime.Now.Year, 1, 1), new DateTime(DateTime.Now.Year, 12, 31))
            }
        },
        {
            "5020",
            new List<BenefitPlan>
            {
                new BenefitPlan("Health Insurance Standard", BenefitPlanTypes[0], "Employee + Spouse", new DateTime(DateTime.Now.Year, 1, 1), new DateTime(DateTime.Now.Year, 12, 31)),
                new BenefitPlan("Vision Insurance", BenefitPlanTypes[2], "Employee Only", new DateTime(DateTime.Now.Year, 1, 1), new DateTime(DateTime.Now.Year, 12, 31))
            }
        },
        {
            "3050",
            new List<BenefitPlan>
            {
                new BenefitPlan("Health Insurance Basic", BenefitPlanTypes[0], "Employee Only", new DateTime(DateTime.Now.Year, 1, 1), new DateTime(DateTime.Now.Year, 12, 31)),
                new BenefitPlan("401k Plan", BenefitPlanTypes[3], "4% Match", new DateTime(DateTime.Now.Year, 1, 1), new DateTime(DateTime.Now.Year, 12, 31))
            }
        }
    };

    // In-memory storage for time off requests
    public static readonly List<TimeOffRequest> TimeOffRequests = new();
}