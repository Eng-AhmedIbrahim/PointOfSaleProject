namespace POS.Core.Repository.Contract;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetByIdWithSpecAsync(ISpecifications<T> specs);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> GetAllWithSpecAsync(ISpecifications<T>spec);    
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<int> GetCountAsync(ISpecifications<T> spec);
}