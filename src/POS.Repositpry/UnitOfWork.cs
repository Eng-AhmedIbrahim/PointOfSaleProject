namespace POS.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    private readonly Hashtable _repositories;
    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
        _repositories = [];
    }
    public IGenericRepository<T> Repository<T>() where T : BaseEntity
        => GetOrCreateRepository<IGenericRepository<T>, GenericRepository<T>>();

    private TRepo GetOrCreateRepository<TRepo, TConcreteRepo>()
        where TConcreteRepo : TRepo
    {
        var key = typeof(TRepo).FullName;

        if (!_repositories.ContainsKey(key!))
        {
            var repository = (TRepo)Activator.CreateInstance(typeof(TConcreteRepo), _dbContext)!;
            _repositories.Add(key!, repository);
        }

        return (TRepo)_repositories[key!]!;
    }

    public async Task<int> CompleteAsync()
         => await _dbContext.SaveChangesAsync();

    public async ValueTask DisposeAsync()
        => await _dbContext.DisposeAsync();
}