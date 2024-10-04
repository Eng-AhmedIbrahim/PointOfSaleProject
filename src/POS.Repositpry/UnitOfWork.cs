namespace POS.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    private Hashtable _repositories;
    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
        _repositories = new Hashtable();
    }
    public IGenericRepository<T>? Repository<T>() where T : BaseEntity
    {
        var EntityType = typeof(T).Name;

        if(!_repositories.ContainsKey(EntityType)) 
        {
            var repo = new GenericRepository<T>(_dbContext);
            _repositories.Add(EntityType, repo);
        }
         return _repositories[EntityType] as IGenericRepository<T>;
    }

    public async Task<int> CompleteAsync()
         => await _dbContext.SaveChangesAsync();

    public async ValueTask DisposeAsync()
     => await _dbContext.DisposeAsync();
}