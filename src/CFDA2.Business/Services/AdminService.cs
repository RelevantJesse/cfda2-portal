using CFDA2.Server.Data;
using CFDA2.Shared.Models;
using CFDA2.Shared.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Portal.Server.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly StripeServiceWrapper _stripe;
    public AdminService(AppDbContext db, UserManager<IdentityUser> userManager, StripeServiceWrapper stripe)
    {
        _db = db;
        _userManager = userManager;
        _stripe = stripe;
    }

    public async Task<IReadOnlyList<FamilyAdminView>> GetFamiliesAsync()
    {
        var balances = await _db.FamilyBalances.ToListAsync();
        var families = await _db.Families.ToListAsync();
        return families.Select(f => new FamilyAdminView
        {
            Id = f.Id,
            Name = f.Name,
            Email = f.Email,
            Phone = f.Phone,
            BalanceCents = balances.FirstOrDefault(b => b.FamilyId == f.Id)?.BalanceCents ?? 0
        }).ToList();
    }

    public async Task<FamilyAdminView> CreateFamilyAsync(FamilyCreateRequest request)
    {
        var family = new Family
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            CreatedUtc = DateTime.UtcNow
        };
        _db.Families.Add(family);
        await _db.SaveChangesAsync();
        var user = new IdentityUser { UserName = request.Email, Email = request.Email, EmailConfirmed = true };
        var password = "Temp1234!";
        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Family");
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("FamilyId", family.Id.ToString()));
        }
        return new FamilyAdminView
        {
            Id = family.Id,
            Name = family.Name,
            Email = family.Email,
            Phone = family.Phone,
            BalanceCents = 0
        };
    }

    public async Task<IReadOnlyList<StudentAdminView>> GetStudentsAsync(int familyId)
    {
        var students = await _db.Students.Where(s => s.FamilyId == familyId).ToListAsync();
        return students.Select(s => new StudentAdminView
        {
            Id = s.Id,
            FamilyId = s.FamilyId,
            FirstName = s.FirstName,
            LastName = s.LastName,
            Birthdate = s.Birthdate,
            Active = s.Active
        }).ToList();
    }

    public async Task<StudentAdminView> CreateStudentAsync(StudentCreateRequest request)
    {
        var student = new Student
        {
            FamilyId = request.FamilyId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Birthdate = request.Birthdate,
            Active = true
        };
        _db.Students.Add(student);
        await _db.SaveChangesAsync();
        return new StudentAdminView
        {
            Id = student.Id,
            FamilyId = student.FamilyId,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Birthdate = student.Birthdate,
            Active = student.Active
        };
    }

    public async Task<IReadOnlyList<ClassView>> GetClassesAsync()
    {
        var classes = await _db.Classes.Include(c => c.Pricing).ToListAsync();
        return classes.Select(c => new ClassView
        {
            Id = c.Id,
            Name = c.Name,
            Level = c.Level,
            Style = c.Style,
            DayOfWeek = c.DayOfWeek,
            StartTime = c.StartTime,
            EndTime = c.EndTime,
            Capacity = c.Capacity,
            Active = c.Active,
            MonthlyTuitionCents = c.Pricing?.MonthlyTuitionCents ?? 0
        }).ToList();
    }

    public async Task<ClassView> CreateClassAsync(ClassCreateRequest request)
    {
        var c = new Class
        {
            Name = request.Name,
            Level = request.Level,
            Style = request.Style,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Capacity = request.Capacity,
            Active = request.Active,
            Pricing = new ClassPricing { MonthlyTuitionCents = request.MonthlyTuitionCents }
        };
        _db.Classes.Add(c);
        await _db.SaveChangesAsync();
        return new ClassView
        {
            Id = c.Id,
            Name = c.Name,
            Level = c.Level,
            Style = c.Style,
            DayOfWeek = c.DayOfWeek,
            StartTime = c.StartTime,
            EndTime = c.EndTime,
            Capacity = c.Capacity,
            Active = c.Active,
            MonthlyTuitionCents = c.Pricing.MonthlyTuitionCents
        };
    }

    public async Task PostChargeAsync(ChargeCreateRequest request)
    {
        var family = await _db.Families.FirstOrDefaultAsync(f => f.Id == request.FamilyId);
        if (family == null) throw new Exception("Family not found");
        var charge = new Charge
        {
            FamilyId = request.FamilyId,
            PostedUtc = DateTime.UtcNow,
            Kind = request.Kind,
            AmountCents = request.AmountCents,
            Memo = request.Memo
        };
        _db.Charges.Add(charge);
        _db.LedgerEntries.Add(new LedgerEntry
        {
            FamilyId = request.FamilyId,
            PostedUtc = DateTime.UtcNow,
            Type = LedgerEntryType.Debit,
            AmountCents = request.AmountCents,
            Memo = request.Memo
        });
        await _db.SaveChangesAsync();
    }

    public async Task PostManualPaymentAsync(ManualPaymentRequest request)
    {
        var family = await _db.Families.FirstOrDefaultAsync(f => f.Id == request.FamilyId);
        if (family == null) throw new Exception("Family not found");
        var payment = new Payment
        {
            FamilyId = request.FamilyId,
            PostedUtc = DateTime.UtcNow,
            Processor = PaymentProcessor.Stripe,
            ProcessorPaymentIntentId = string.Empty,
            AmountCents = request.AmountCents,
            Memo = request.Memo,
            Status = "manual"
        };
        _db.Payments.Add(payment);
        _db.LedgerEntries.Add(new LedgerEntry
        {
            FamilyId = request.FamilyId,
            PostedUtc = DateTime.UtcNow,
            Type = LedgerEntryType.Credit,
            AmountCents = request.AmountCents,
            Memo = request.Memo
        });
        await _db.SaveChangesAsync();
    }

    public async Task<int> GenerateInvoicesAsync(DateTime from, DateTime to)
    {
        var families = await _db.Families.Include(f => f.Students).ThenInclude(s => s.Enrollments).ThenInclude(e => e.Class).ThenInclude(c => c.Pricing).ToListAsync();
        int count = 0;
        foreach (var family in families)
        {
            var invoice = new Invoice
            {
                FamilyId = family.Id,
                PeriodStartUtc = from,
                PeriodEndUtc = to,
                Status = InvoiceStatus.Draft,
                CreatedUtc = DateTime.UtcNow
            };
            _db.Invoices.Add(invoice);
            int total = 0;
            foreach (var student in family.Students)
            {
                foreach (var enrollment in student.Enrollments)
                {
                    if (enrollment.Status == EnrollmentStatus.Active)
                    {
                        var tuition = enrollment.Class.Pricing?.MonthlyTuitionCents ?? 0;
                        total += tuition;
                        var charge = new Charge
                        {
                            FamilyId = family.Id,
                            Invoice = invoice,
                            PostedUtc = DateTime.UtcNow,
                            Kind = ChargeKind.Tuition,
                            AmountCents = tuition,
                            Memo = $"Tuition for {enrollment.Class.Name}"
                        };
                        _db.Charges.Add(charge);
                        _db.LedgerEntries.Add(new LedgerEntry
                        {
                            FamilyId = family.Id,
                            Invoice = invoice,
                            PostedUtc = DateTime.UtcNow,
                            Type = LedgerEntryType.Debit,
                            AmountCents = tuition,
                            Memo = charge.Memo
                        });
                    }
                }
            }
            invoice.TotalCents = total;
            count++;
        }
        await _db.SaveChangesAsync();
        return count;
    }

    public async Task FinalizeInvoiceAsync(int invoiceId)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice == null) throw new Exception("Invoice not found");
        invoice.Status = InvoiceStatus.Finalized;
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AgingReportRow>> GetAgingReportAsync()
    {
        var view = await _db.AgingBuckets.ToListAsync();
        var families = await _db.Families.ToListAsync();
        return view.Join(families, v => v.FamilyId, f => f.Id, (v, f) => new AgingReportRow
        {
            FamilyId = f.Id,
            FamilyName = f.Name,
            CurrentCents = v.CurrentCents,
            Over30Cents = v.Over30Cents,
            Over60Cents = v.Over60Cents,
            Over90Cents = v.Over90Cents
        }).ToList();
    }

    public async Task<IReadOnlyList<RevenueReportRow>> GetRevenueReportAsync(DateTime from, DateTime to)
    {
        var payments = await _db.Payments.Where(p => p.PostedUtc >= from && p.PostedUtc <= to).ToListAsync();
        var grouped = payments.GroupBy(p => p.PostedUtc.Date)
            .Select(g => new RevenueReportRow { Date = g.Key, RevenueCents = g.Sum(p => p.AmountCents) })
            .OrderBy(r => r.Date)
            .ToList();
        return grouped;
    }
}