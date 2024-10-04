namespace POS.Core.Specifications;

public class BaseSpecifications<T> : ISpecifications<T> where T : BaseEntity
{
    public Expression<Func<T, bool>> Criteria { get; set; } = null;
    public List<Expression<Func<T, object>>> Includes { get ; set ; }  = new List<Expression<Func<T, object>>> ();
    public Expression<Func<T, object>> OrderBy { get; set; } = null;
    public Expression<Func<T, object>> OrderByDesc { get; set; } = null;
    public int? Take { get ; set ; }
    public int? Skip { get ; set ; }
    public bool IsPaginationEnabled { get ; set ; }

    public BaseSpecifications()
    {
        //No Criteria
    }

    public BaseSpecifications(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    public void AddOrderBy(Expression<Func<T, object>> orderBy) =>
        OrderBy = orderBy;

    public void AddOrderByDesc(Expression<Func<T, object>> orderByDesc) =>
        OrderByDesc = orderByDesc;

    public void EnablePagination(int? skip,int? take)
    {
        IsPaginationEnabled = true;
        Skip = skip;
        Take = take;
    }
}