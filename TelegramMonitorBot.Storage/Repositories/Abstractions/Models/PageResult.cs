namespace TelegramMonitorBot.Storage.Repositories.Abstractions.Models;

public class PageResult<TItem> : List<TItem>
{
    internal static readonly PageResult<TItem> EmptyPageResult = new (Array.Empty<TItem>(), 0, 0);
    
    public int PageNumber { get; }
    public int PagesCount { get; }

    // TODO remove
    public PageResult(IEnumerable<TItem> items, int pageNumber, int pagesCount) : base(items)
    {
        PageNumber = pageNumber;
        PagesCount = pagesCount;
    }

    public PageResult(ICollection<TItem> items, Pager pager) 
        : base(items.Skip(pager.PageSize * (pager.Page - 1)).Take(pager.PageSize))
    {
        PageNumber = pager.Page;
        PagesCount = items.Count / pager.PageSize + (items.Count % pager.PageSize == 0 ? 0 : 1);
    }
}