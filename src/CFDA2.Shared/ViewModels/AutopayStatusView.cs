namespace CFDA2.Shared.ViewModels;

public class AutopayStatusView
{
    public bool Enabled { get; set; }
    public int? DefaultPaymentMethodId { get; set; }
    public int DraftDay { get; set; }
    public int GraceDays { get; set; }
}