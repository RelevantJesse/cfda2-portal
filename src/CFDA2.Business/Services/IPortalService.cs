using Portal.Shared.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal.Server.Services;

public interface IPortalService
{
    Task<BalanceView> GetBalanceAsync(string userId);
    Task<PagedResult<LedgerEntryView>> GetLedgerAsync(string userId, int page, int size);
    Task<IReadOnlyList<PaymentMethodView>> GetPaymentMethodsAsync(string userId);
    Task<SetupIntentView> CreateSetupIntentAsync(string userId);
    Task AttachPaymentMethodAsync(string userId, string paymentMethodId);
    Task<PaymentResultView> OneTimePaymentAsync(string userId, int paymentMethodId, int amountCents);
    Task<AutopayStatusView> GetAutopayStatusAsync(string userId);
    Task EnableAutopayAsync(string userId, int paymentMethodId, int draftDay, int graceDays);
    Task DisableAutopayAsync(string userId);
    Task SetDefaultPaymentMethodAsync(string userId, int paymentMethodId);
    Task<IReadOnlyList<ClassView>> GetClassesAsync();
    Task EnrollAsync(string userId, int studentId, int classId);
}