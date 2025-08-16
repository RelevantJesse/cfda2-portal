using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portal.Server.Data;
using Portal.Shared.ViewModels;
using Portal.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Portal.Server.Services;

public class PortalService : IPortalService
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly StripeServiceWrapper _stripe;
    public PortalService(AppDbContext db, UserManager<IdentityUser> userManager, StripeServiceWrapper stripe)
    {
        _db = db;
        _userManager = userManager;
        _stripe = stripe;
    }

    private async Task<Family?> GetFamilyAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;
        var claims = await _userManager.GetClaimsAsync(user);
        var familyClaim = claims.FirstOrDefault(c => c.Type == "FamilyId");
        if (familyClaim == null) return null;
        if (!int.TryParse(familyClaim.Value, out var fid)) return null;
        return await _db.Families.Include(f => f.AutopaySettings).Include(f => f.PaymentMethods).FirstOrDefaultAsync(f => f.Id == fid);
    }

    public async Task<BalanceView> GetBalanceAsync(string userId)
    {
        var family = await GetFamilyAsync(userId);
        if (family == null) throw new Exception("Family not found");
        var balance = await _db.FamilyBalances.FirstOrDefaultAsync(f => f.FamilyId == family.Id);
        return new BalanceView { BalanceCents = balance?.BalanceCents ?? 0 };
    }

    public async Task<PagedResult<LedgerEntryView>> GetLedgerAsync(string userId, int page, int size)
    {
        var family = await GetFamilyAsync(userId);
        if (family == null) throw new Exception("Family not found");
        var query = _db.LedgerEntries.Where(l => l.FamilyId == family.Id).OrderByDescending(l => l.PostedUtc);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * size).Take(size).Select(l => new LedgerEntryView
        {
            Date = l.PostedUtc,
            Type = l.Type.ToString(),
            AmountCents = l.AmountCents,
            Memo = l.Memo
        }).ToListAsync();
        return new PagedResult<LedgerEntryView> { Items = items, Total = total };
    }

    public async Task<IReadOnlyList<PaymentMethodView>> GetPaymentMethodsAsync(string userId)
    {
        var family = await GetFamilyAsync(userId);
        if (family == null) throw new Exception("Family not found");
        return family.PaymentMethods.Select(pm => new PaymentMethodView
        {
            Id = pm.Id,
            Brand = pm.Brand,
            Last4 = pm.Last4,
            ExpMonth = pm.ExpMonth,
            ExpYear = pm.ExpYear,
            Type = pm.Type.ToString(),
            IsDefault = pm.IsDefault
        }).ToList();
    }

    public async Task<SetupIntentView> CreateSetupIntentAsync(string userId)
    {
        var family = await GetFamilyAsync(userId);
        if (family == null || string.IsNullOrWhiteSpace(family.StripeCustomerId)) throw new Exception("Family not configured for Stripe");
        var intent = await _stripe.CreateSetupIntentAsync(family.StripeCustomerId);
        return new SetupIntentView { ClientSecret = intent.ClientSecret };
    }

    public async Task AttachPaymentMethodAsync(string userId, string paymentMethodId)
    {
        var family = await GetFamilyAsync(userId);
        if (family == null || string.IsNullOrWhiteSpace(family.StripeCustomerId)) throw new Exception("Family not found or not configured");
        var method = await _stripe.RetrievePaymentMethodAsync(paymentMethodId);
        var pm = new PaymentMethod
        {
            FamilyId = family.Id,
            Processor = PaymentProcessor.Stripe,
            ProcessorPaymentMethodId = method.Id,
            Brand = method.Card?.Brand ?? "",
            Last4 = method.Card?.Last4 ?? "",
            ExpMonth = (int)(method.Card?.ExpMonth ?? 0),
            ExpYear = (int)(method.Card?.ExpYear ?? 0),
            Type = PaymentMethodType.Card,
            IsDefault = family.PaymentMethods.Count == 0,
            CreatedUtc = DateTime.UtcNow
        };
        _db.PaymentMethods.Add(pm);
        if (pm.IsDefault)
        {
            family.AutopaySettings ??= new AutopaySettings { FamilyId = family.Id, Enabled = false, DraftDay = 1, GraceDays = 0 };
            family.AutopaySettings.DefaultPaymentMethodId = pm.Id;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<PaymentResultView> OneTimePaymentAsync(string userId, int paymentMethodId, int amountCents)
    {
        var family = await GetFamilyAsync(userId);
        if (family == null) throw new Exception("Family not found");
        var method = await _db.PaymentMethods.FirstOrDefaultAsync(pm => pm.Id == paymentMethodId && pm.FamilyId == family.Id);
        if (method == null) throw new Exception("Payment method not found");
        if (string.IsNullOrWhiteSpace(family.StripeCustomerId)) throw new Exception("Stripe customer missing");
        var intent = await _stripe.CreatePaymentIntentAsync(family.StripeCustomerId, method.ProcessorPaymentMethodId, amountCents);
        var payment = new Payment
        {
            FamilyId = family.Id,
            PostedUtc = DateTime.UtcNow,
            Processor = PaymentProcessor.Stripe,
            ProcessorPaymentIntentId = intent.Id,
            AmountCents = amountCents,
            Memo = "One time payment",
            Status = intent.Status
        };
        _db.Payments.Add(payment);
        _db.LedgerEntries.Add(new LedgerEntry
        {
            FamilyId = family.Id,
            PostedUtc = DateTime.UtcNow,
            Type = LedgerEntryType.Credit,
            AmountCents = amountCents,
            Memo = "Payment"
        });
        await _db.SaveChangesAsync();
        return new PaymentResultView { PaymentId = payment.Id, Status = payment.Status };
    }

    public async Task<AutopayStatusView> GetAutopayStatusAsync(string userId)
    {
        var family = await GetFamilyAsync(userId);
        if (family == null) throw new Exception("Family not found");
        var settings = family.AutopaySettings;
        return new AutopayStatusView
        {
            Enabled = settings?.Enabled ?? false,
            DefaultPaymentMethodId = settings?.DefaultPaymentMethodId,
            DraftDay = settings?.DraftDay ?? 1,
            GraceDays = settings?.GraceDays ?? 0
        };
    }

    public async Task EnableAutopayAsync(string userId, int paymentMethodId, int draftDay, int graceDays)
    {
        var family = await GetFamilyAsync(userId);
        if (family == null) throw new Exception("Family not found");
        var method = await _db.PaymentMethods.FirstOrDefaultAsync(pm => pm.Id == paymentMethodId && pm.FamilyId == family.Id);
        if (method == null) throw new Exception("Payment method not found");
        family.AutopaySettings ??= new AutopaySettings { FamilyId = family.Id };
        family.AutopaySettings.Enabled = true;
        family.AutopaySettings.DefaultPaymentMethodId = paymentMethodId;
        family.AutopaySettings.DraftDay = draftDay;
        family.AutopaySettings.GraceDays = graceDays;
        method.IsDefault = true;
        foreach (var m in family.PaymentMethods.Where(p => p.Id != method.Id))
        {
            m.IsDefault = false;
        }
        await _db.SaveChangesAsync();
    }

    public async Task DisableAutopayAsync(string userId)
    {
        var family = await GetFamilyAsync(userId);
        if (family == null) throw new Exception("Family not found");
        if (family.AutopaySettings != null)
        {
            family.AutopaySettings.Enabled = false;
            await _db.SaveChangesAsync();
        }
    }

    public async Task SetDefaultPaymentMethodAsync(string userId, int paymentMethodId)
    {
        var family = await GetFamilyAsync(userId);
        if (family == null) throw new Exception("Family not found");
        var method = family.PaymentMethods.FirstOrDefault(pm => pm.Id == paymentMethodId);
        if (method == null) throw new Exception("Method not found");
        foreach (var m in family.PaymentMethods)
        {
            m.IsDefault = false;
        }
        method.IsDefault = true;
        if (family.AutopaySettings != null)
        {
            family.AutopaySettings.DefaultPaymentMethodId = paymentMethodId;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ClassView>> GetClassesAsync()
    {
        var classes = await _db.Classes.Include(c => c.Pricing).Where(c => c.Active).ToListAsync();
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

    public async Task EnrollAsync(string userId, int studentId, int classId)
    {
        var family = await GetFamilyAsync(userId);
        if (family == null) throw new Exception("Family not found");
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId && s.FamilyId == family.Id);
        var @class = await _db.Classes.FirstOrDefaultAsync(c => c.Id == classId && c.Active);
        if (student == null || @class == null) throw new Exception("Student or class not found");
        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            ClassId = @class.Id,
            StartDate = DateTime.UtcNow,
            Status = EnrollmentStatus.Active
        };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();
    }
}