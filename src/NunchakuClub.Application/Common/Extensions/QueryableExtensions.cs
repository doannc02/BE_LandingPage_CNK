using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Common.Extensions;

/// <summary>
/// Wrapper cho paginated response - flat structure khi serialize JSON
/// </summary>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }

    public static PaginatedResult<T> FromPaginatedList(PaginatedList<T> paginatedList)
    {
        return new PaginatedResult<T>
        {
            Items = paginatedList.Items,
            PageNumber = paginatedList.PageNumber,
            PageSize = paginatedList.PageSize,
            TotalCount = paginatedList.TotalCount,
            TotalPages = paginatedList.TotalPages,
            HasPrevious = paginatedList.HasPrevious,
            HasNext = paginatedList.HasNext
        };
    }
}

public static class QueryableExtensions
{
    public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = count
        };
    }
}
