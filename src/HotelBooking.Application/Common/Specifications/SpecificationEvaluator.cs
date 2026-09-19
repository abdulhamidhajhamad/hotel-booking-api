using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Common.Specifications;

public static class SpecificationEvaluator
{
    public static IQueryable<T> GetQuery<T>(IQueryable<T> input, Specification<T> specification)
        where T : class
    {
        var query = input;

        if (specification.IgnoreQueryFilters)
            query = query.IgnoreQueryFilters();

        if (specification.AsNoTracking)
            query = query.AsNoTracking();

        if (specification.AsSplitQuery)
            query = query.AsSplitQuery();

        if (specification.Criteria is not null)
            query = query.Where(specification.Criteria);

        query = specification.Includes.Aggregate(
            query,
            (current, include) => current.Include(include));

        query = specification.IncludeStrings.Aggregate(
            query,
            (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
            query = specification.OrderByDescending
                ? query.OrderByDescending(specification.OrderBy)
                : query.OrderBy(specification.OrderBy);

        if (specification.IsPagingEnabled)
            query = query
                .Skip(specification.Skip ?? 0)
                .Take(specification.Take ?? int.MaxValue);

        return query;
    }

    public static IQueryable<T> GetCountQuery<T>(IQueryable<T> input, Specification<T> specification)
        where T : class
    {
        var query = input;

        if (specification.IgnoreQueryFilters)
            query = query.IgnoreQueryFilters();

        if (specification.Criteria is not null)
            query = query.Where(specification.Criteria);

        return query;
    }
}