namespace CFDA2.Shared.Models;

public class Family
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? StripeCustomerId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
    public AutopaySettings? AutopaySettings { get; set; }
    public ICollection<Charge> Charges { get; set; } = new List<Charge>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

public class Student
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime Birthdate { get; set; }
    public bool Active { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

public class Class
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int Capacity { get; set; }
    public bool Active { get; set; }
    public ClassPricing? Pricing { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public EnrollmentStatus Status { get; set; }
}

public class PaymentMethod
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public PaymentProcessor Processor { get; set; }
    public string ProcessorPaymentMethodId { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public int ExpMonth { get; set; }
    public int ExpYear { get; set; }
    public PaymentMethodType Type { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class AutopaySettings
{
    public int FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public bool Enabled { get; set; }
    public int? DefaultPaymentMethodId { get; set; }
    public PaymentMethod? DefaultPaymentMethod { get; set; }
    public int DraftDay { get; set; }
    public int GraceDays { get; set; }
}

public class Charge
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public DateTime PostedUtc { get; set; }
    public ChargeKind Kind { get; set; }
    public int AmountCents { get; set; }
    public string Memo { get; set; } = string.Empty;
}

public class Payment
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public DateTime PostedUtc { get; set; }
    public PaymentProcessor Processor { get; set; }
    public string ProcessorPaymentIntentId { get; set; } = string.Empty;
    public int AmountCents { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class LedgerEntry
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public DateTime PostedUtc { get; set; }
    public LedgerEntryType Type { get; set; }
    public int AmountCents { get; set; }
    public string Memo { get; set; } = string.Empty;
}

public class Invoice
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public InvoiceStatus Status { get; set; }
    public int TotalCents { get; set; }
    public DateTime CreatedUtc { get; set; }
    public ICollection<Charge> Charges { get; set; } = new List<Charge>();
    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}

public class Discount
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public int? ValueCents { get; set; }
    public decimal? Percent { get; set; }
    public string CriteriaJson { get; set; } = string.Empty;
    public bool Active { get; set; }
}

public class ClassPricing
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public int MonthlyTuitionCents { get; set; }
}

public class FamilyBalanceView
{
    public int FamilyId { get; set; }
    public int BalanceCents { get; set; }
}

public class AgingBucketView
{
    public int FamilyId { get; set; }
    public int CurrentCents { get; set; }
    public int Over30Cents { get; set; }
    public int Over60Cents { get; set; }
    public int Over90Cents { get; set; }
}