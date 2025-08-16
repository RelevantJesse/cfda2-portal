namespace CFDA2.Shared.Models;

public enum PaymentProcessor
{
    Stripe = 1
}

public enum PaymentMethodType
{
    Card = 1,
    Bank = 2
}

public enum EnrollmentStatus
{
    Active = 1,
    Completed = 2,
    Canceled = 3
}

public enum ChargeKind
{
    Tuition = 1,
    Fee = 2,
    Adjustment = 3
}

public enum LedgerEntryType
{
    Debit = 1,
    Credit = 2
}

public enum InvoiceStatus
{
    Draft = 1,
    Finalized = 2,
    Paid = 3,
    Failed = 4
}