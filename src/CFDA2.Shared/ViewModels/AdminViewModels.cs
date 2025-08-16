using CFDA2.Shared.Models;

namespace CFDA2.Shared.ViewModels;

public class FamilyAdminView
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int BalanceCents { get; set; }
}

public class FamilyCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class StudentAdminView
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime Birthdate { get; set; }
    public bool Active { get; set; }
}

public class StudentCreateRequest
{
    public int FamilyId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime Birthdate { get; set; }
}

public class ClassCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int Capacity { get; set; }
    public bool Active { get; set; }
    public int MonthlyTuitionCents { get; set; }
}

public class ChargeCreateRequest
{
    public int FamilyId { get; set; }
    public ChargeKind Kind { get; set; }
    public int AmountCents { get; set; }
    public string Memo { get; set; } = string.Empty;
}

public class ManualPaymentRequest
{
    public int FamilyId { get; set; }
    public int AmountCents { get; set; }
    public string Memo { get; set; } = string.Empty;
}

public class AgingReportRow
{
    public int FamilyId { get; set; }
    public string FamilyName { get; set; } = string.Empty;
    public int CurrentCents { get; set; }
    public int Over30Cents { get; set; }
    public int Over60Cents { get; set; }
    public int Over90Cents { get; set; }
}

public class RevenueReportRow
{
    public DateTime Date { get; set; }
    public int RevenueCents { get; set; }
}