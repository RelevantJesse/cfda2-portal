namespace CFDA2.Shared.ViewModels;

public class LedgerEntryView
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public int AmountCents { get; set; }
    public string Memo { get; set; } = string.Empty;
}