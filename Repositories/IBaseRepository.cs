using Microsoft.EntityFrameworkCore;

namespace TeckNews.Repositories
{
    public interface IBaseRepository<TEntity>
        where TEntity : class
    {
        DbSet<TEntity> Entities { get; }
        IQueryable<TEntity> Table { get; }
        IQueryable<TEntity> TableNoTracking { get; }


        ValueTask<TEntity> GetByIdAsync(CancellationToken cancellationToken, params object[] ids);
        Task AddAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);
        Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);
        Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
        
        TEntity GetById(params object[] ids);
        void Add(TEntity entity, bool saveNow = true);
        void AddRange(IEnumerable<TEntity> entities, bool saveNow = true);
        void Update(TEntity entity, bool saveNow = true);
        void UpdateRange(IEnumerable<TEntity> entities, bool saveNow = true);
        void Delete(TEntity entity, bool saveNow = true);
        void DeleteRange(IEnumerable<TEntity> entities, bool saveNow = true);
        
        void Attach(TEntity entity);
        void Detach(TEntity entity);
    }
}
