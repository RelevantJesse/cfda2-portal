using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CFDA2.Shared.Models;

namespace CFDA2.Server.Data;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Family> Families => Set<Family>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<AutopaySettings> AutopaySettings => Set<AutopaySettings>();
    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<ClassPricing> ClassPricings => Set<ClassPricing>();
    public DbSet<FamilyBalanceView> FamilyBalances => Set<FamilyBalanceView>();
    public DbSet<AgingBucketView> AgingBuckets => Set<AgingBucketView>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Family>(entity =>
        {
            entity.ToTable("Families");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).IsRequired();
            entity.Property(f => f.Email).IsRequired();
        });

        builder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(s => s.Id);
            entity.HasOne(s => s.Family).WithMany(f => f.Students).HasForeignKey(s => s.FamilyId);
        });

        builder.Entity<Class>(entity =>
        {
            entity.ToTable("Classes");
            entity.HasKey(c => c.Id);
            entity.HasOne(c => c.Pricing).WithOne(p => p.Class).HasForeignKey<ClassPricing>(p => p.ClassId);
        });

        builder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("Enrollments");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Student).WithMany(s => s.Enrollments).HasForeignKey(e => e.StudentId);
            entity.HasOne(e => e.Class).WithMany(c => c.Enrollments).HasForeignKey(e => e.ClassId);
        });

        builder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("PaymentMethods");
            entity.HasKey(pm => pm.Id);
            entity.HasOne(pm => pm.Family).WithMany(f => f.PaymentMethods).HasForeignKey(pm => pm.FamilyId);
        });

        builder.Entity<AutopaySettings>(entity =>
        {
            entity.ToTable("AutopaySettings");
            entity.HasKey(a => a.FamilyId);
            entity.HasOne(a => a.Family).WithOne(f => f.AutopaySettings).HasForeignKey<AutopaySettings>(a => a.FamilyId);
            entity.HasOne(a => a.DefaultPaymentMethod).WithMany().HasForeignKey(a => a.DefaultPaymentMethodId);
        });

        builder.Entity<Charge>(entity =>
        {
            entity.ToTable("Charges");
            entity.HasKey(c => c.Id);
            entity.HasOne(c => c.Family).WithMany(f => f.Charges).HasForeignKey(c => c.FamilyId);
            entity.HasOne(c => c.Invoice).WithMany(i => i.Charges).HasForeignKey(c => c.InvoiceId);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(p => p.Id);
            entity.HasOne(p => p.Family).WithMany(f => f.Payments).HasForeignKey(p => p.FamilyId);
        });

        builder.Entity<LedgerEntry>(entity =>
        {
            entity.ToTable("LedgerEntries");
            entity.HasKey(l => l.Id);
            entity.HasOne(l => l.Family).WithMany(f => f.LedgerEntries).HasForeignKey(l => l.FamilyId);
            entity.HasOne(l => l.Invoice).WithMany(i => i.LedgerEntries).HasForeignKey(l => l.InvoiceId);
        });

        builder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasKey(i => i.Id);
            entity.HasOne(i => i.Family).WithMany(f => f.Invoices).HasForeignKey(i => i.FamilyId);
        });

        builder.Entity<Discount>(entity =>
        {
            entity.ToTable("Discounts");
            entity.HasKey(d => d.Id);
        });

        builder.Entity<ClassPricing>(entity =>
        {
            entity.ToTable("ClassPricing");
            entity.HasKey(cp => cp.Id);
        });

        builder.Entity<FamilyBalanceView>().HasNoKey().ToView("v_FamilyBalances");
        builder.Entity<AgingBucketView>().HasNoKey().ToView("v_AgingBuckets");
    }
}