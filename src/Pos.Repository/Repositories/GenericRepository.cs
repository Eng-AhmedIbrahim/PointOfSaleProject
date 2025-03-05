namespace Pos.Repository.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    private readonly AppDbContext _dbContext;

    public GenericRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(T entity)
        => await _dbContext.Set<T>().AddAsync(entity);

    public async Task<IReadOnlyList<T>> GetAllAsync()
        => await _dbContext.Set<T>().AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<T>> GetAllWithSpecificationAsync(ISpecifications<T> spec)
        => await ApplySpecification(spec).ToListAsync();

    public async Task<T?> GetByIdAsync(int id)
        => await _dbContext.Set<T>().FindAsync(id);

    public async Task<T?> GetByIdWithSpecificationAsync(ISpecifications<T> specs)
        => await ApplySpecification(specs).FirstOrDefaultAsync();

    public async Task<int> GetCountAsync(ISpecifications<T> spec)
        => await ApplySpecification(spec).CountAsync();

    public void Delete(T entity)
    {
        if (_dbContext.Entry(entity).State == EntityState.Detached)
            _dbContext.Set<T>().Attach(entity);

        _dbContext.Set<T>().Remove(entity);
    }

    public void Update(T entity)
        => _dbContext.Update(entity);

    public async Task AddRangeAsync(IEnumerable<T> entities)
        => await _dbContext.Set<T>().AddRangeAsync(entities);

    public void UpdateRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
        {
            var local = _dbContext.Set<T>()
                .Local
                .FirstOrDefault(entry => entry.Id.Equals(entity.Id));

            if (local is not null)
                _dbContext.Entry(local).State = EntityState.Detached;

            _dbContext.Entry(entity).State = EntityState.Modified;
        }
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        => await _dbContext.Set<T>().AnyAsync(predicate);

    IQueryable<T> ApplySpecification(ISpecifications<T> spec)
        => SpecificationEvaluator<T>.GetQuery(_dbContext.Set<T>().AsNoTracking(), spec);

    public async Task<T?> GetUserSettingByIdAsync(string id)
    => await _dbContext.Set<T>(id).FirstOrDefaultAsync();
}
