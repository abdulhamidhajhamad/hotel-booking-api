using System.Linq.Expressions;

namespace HotelBooking.Application.Common.Specifications;

public abstract class Specification<T> where T : class
{
    public Expression<Func<T, bool>>? Criteria { get; protected set; }

    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();

    public Expression<Func<T, object>>? OrderBy { get; protected set; }
    public bool OrderByDescending { get; protected set; }

    public int? Skip { get; protected set; }
    public int? Take { get; protected set; }
    public bool IsPagingEnabled { get; protected set; }

    public bool AsNoTracking { get; protected set; } = true;
    public bool AsSplitQuery { get; protected set; }
    public bool IgnoreQueryFilters { get; protected set; }

    protected void SetCriteria(Expression<Func<T, bool>> criteria) => Criteria = criteria;

    protected void AddInclude(Expression<Func<T, object>> include) => Includes.Add(include);
    protected void AddInclude(string include) => IncludeStrings.Add(include);

    protected void SetOrderBy(Expression<Func<T, object>> orderBy, bool descending = false)
    {
        OrderBy = orderBy;
        OrderByDescending = descending;
    }

    protected void SetPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    protected void EnableSplitQuery() => AsSplitQuery = true;
    protected void EnableTracking() => AsNoTracking = false;
    protected void EnableIgnoreQueryFilters() => IgnoreQueryFilters = true;
}