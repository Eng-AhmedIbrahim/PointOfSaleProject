namespace POS.Repository;

public static class SpecificationEvaluator<T> where T : BaseEntity
{
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery , ISpecifications<T> spec)
    {
        var query = inputQuery;

        if(spec.Criteria != null)
            query = query.Where(spec.Criteria);
        
        if(spec.OrderBy != null)
            query = query.OrderBy(spec.OrderBy);

        if(spec.OrderByDesc != null)
            query = query.OrderByDescending(spec.OrderByDesc);

        if(spec.IsPaginationEnabled)
            query = query.Skip(spec?.Skip??0).Take(spec?.Take??5);

        query = spec?.Includes.Aggregate(query, (currentQuery, includeExpression) => currentQuery.Include(includeExpression)) ?? inputQuery;
    
        return query;
    }
}
