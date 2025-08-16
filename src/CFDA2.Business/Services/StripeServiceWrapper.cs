using Microsoft.Extensions.Options;
using Stripe;

namespace Portal.Server.Services;

public class StripeServiceWrapper
{
    private readonly StripeSettings _settings;
    public StripeServiceWrapper(IOptions<StripeSettings> settings)
    {
        _settings = settings.Value;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<PaymentMethod> RetrievePaymentMethodAsync(string paymentMethodId)
    {
        var service = new PaymentMethodService();
        return await service.GetAsync(paymentMethodId);
    }

    public async Task<SetupIntent> CreateSetupIntentAsync(string customerId)
    {
        var service = new SetupIntentService();
        var options = new SetupIntentCreateOptions
        {
            Customer = customerId,
            PaymentMethodTypes = new List<string> { "card" }
        };
        return await service.CreateAsync(options);
    }

    public async Task<PaymentIntent> CreatePaymentIntentAsync(string customerId, string paymentMethodId, long amountCents)
    {
        var service = new PaymentIntentService();
        var options = new PaymentIntentCreateOptions
        {
            Customer = customerId,
            Amount = amountCents,
            Currency = "usd",
            PaymentMethod = paymentMethodId,
            ConfirmationMethod = "automatic",
            Confirm = true,
            OffSession = false
        };
        return await service.CreateAsync(options);
    }

    public async Task<PaymentIntent> CreateOffSessionPaymentIntentAsync(string customerId, string paymentMethodId, long amountCents)
    {
        var service = new PaymentIntentService();
        var options = new PaymentIntentCreateOptions
        {
            Customer = customerId,
            Amount = amountCents,
            Currency = "usd",
            PaymentMethod = paymentMethodId,
            ConfirmationMethod = "automatic",
            Confirm = true,
            OffSession = true
        };
        return await service.CreateAsync(options);
    }
}