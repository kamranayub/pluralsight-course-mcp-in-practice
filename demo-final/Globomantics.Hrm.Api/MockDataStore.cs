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

    // Benefit plans per employee
    public static readonly Dictionary<string, List<BenefitPlan>> BenefitPlans = new()
    {
        {
            "5050",
            new List<BenefitPlan>
            {
                new BenefitPlan("Health Insurance Premium", "Employee + Family", "2024-01-01", "2024-12-31"),
                new BenefitPlan("Dental Insurance", "Employee Only", "2024-01-01", "2024-12-31"),
                new BenefitPlan("401k Plan", "6% Match", "2024-01-01", "2024-12-31")
            }
        },
        {
            "5020",
            new List<BenefitPlan>
            {
                new BenefitPlan("Health Insurance Standard", "Employee + Spouse", "2024-01-01", "2024-12-31"),
                new BenefitPlan("Vision Insurance", "Employee Only", "2024-01-01", "2024-12-31")
            }
        },
        {
            "3050",
            new List<BenefitPlan>
            {
                new BenefitPlan("Health Insurance Basic", "Employee Only", "2024-01-01", "2024-12-31"),
                new BenefitPlan("401k Plan", "4% Match", "2024-01-01", "2024-12-31")
            }
        }
    };

    // In-memory storage for time off requests
    public static readonly List<TimeOffRequest> TimeOffRequests = new();
}