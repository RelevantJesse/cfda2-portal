using CFDA2.Shared.ViewModels;

namespace Portal.Server.Services;

public interface IAdminService
{
    Task<IReadOnlyList<FamilyAdminView>> GetFamiliesAsync();
    Task<FamilyAdminView> CreateFamilyAsync(FamilyCreateRequest request);
    Task<IReadOnlyList<StudentAdminView>> GetStudentsAsync(int familyId);
    Task<StudentAdminView> CreateStudentAsync(StudentCreateRequest request);
    Task<IReadOnlyList<ClassView>> GetClassesAsync();
    Task<ClassView> CreateClassAsync(ClassCreateRequest request);
    Task PostChargeAsync(ChargeCreateRequest request);
    Task PostManualPaymentAsync(ManualPaymentRequest request);
    Task<int> GenerateInvoicesAsync(DateTime from, DateTime to);
    Task FinalizeInvoiceAsync(int invoiceId);
    Task<IReadOnlyList<AgingReportRow>> GetAgingReportAsync();
    Task<IReadOnlyList<RevenueReportRow>> GetRevenueReportAsync(DateTime from, DateTime to);
}