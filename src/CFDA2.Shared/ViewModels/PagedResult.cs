namespace CFDA2.Shared.ViewModels;

public class PagedResult<T>
{
    public int Total { get; set; }
    public IEnumerable<T> Items { get; set; } = new List<T>();
}