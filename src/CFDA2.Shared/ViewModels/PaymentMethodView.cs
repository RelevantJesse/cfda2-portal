namespace CFDA2.Shared.ViewModels;

public class PaymentMethodView
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public int ExpMonth { get; set; }
    public int ExpYear { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}