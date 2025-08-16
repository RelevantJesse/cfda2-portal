using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CFDA2.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CFDA2.Server.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await context.Database.MigrateAsync();
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }
        if (!await roleManager.RoleExistsAsync("Family"))
        {
            await roleManager.CreateAsync(new IdentityRole("Family"));
        }
        if (!await userManager.Users.AnyAsync(u => u.UserName == "admin@cfda.local"))
        {
            var admin = new IdentityUser { UserName = "admin@cfda.local", Email = "admin@cfda.local", EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, "Admin1234!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
        if (!context.Families.Any())
        {
            var family = new Family
            {
                Name = "Sample Family",
                Email = "family@example.com",
                Phone = "000-000-0000",
                CreatedUtc = DateTime.UtcNow,
                StripeCustomerId = null
            };
            context.Families.Add(family);
            var student = new Student
            {
                Family = family,
                FirstName = "Student",
                LastName = "One",
                Birthdate = DateTime.UtcNow.AddYears(-10),
                Active = true
            };
            context.Students.Add(student);
            var cls = new Class
            {
                Name = "Ballet Basics",
                Level = "Beginner",
                Style = "Ballet",
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(17, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                Capacity = 20,
                Active = true,
                Pricing = new ClassPricing { MonthlyTuitionCents = 5000 }
            };
            context.Classes.Add(cls);
            var enrollment = new Enrollment
            {
                Student = student,
                Class = cls,
                StartDate = DateTime.UtcNow,
                Status = EnrollmentStatus.Active
            };
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();
            var invoice = new Invoice
            {
                FamilyId = family.Id,
                PeriodStartUtc = DateTime.UtcNow.AddMonths(-1),
                PeriodEndUtc = DateTime.UtcNow,
                Status = InvoiceStatus.Draft,
                TotalCents = 5000,
                CreatedUtc = DateTime.UtcNow
            };
            context.Invoices.Add(invoice);
            var charge = new Charge
            {
                FamilyId = family.Id,
                Invoice = invoice,
                PostedUtc = DateTime.UtcNow,
                Kind = ChargeKind.Tuition,
                AmountCents = 5000,
                Memo = "Tuition"
            };
            context.Charges.Add(charge);
            var ledger = new LedgerEntry
            {
                FamilyId = family.Id,
                Invoice = invoice,
                PostedUtc = DateTime.UtcNow,
                Type = LedgerEntryType.Debit,
                AmountCents = 5000,
                Memo = "Opening charge"
            };
            context.LedgerEntries.Add(ledger);
            await context.SaveChangesAsync();
        }
    }
}