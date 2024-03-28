using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TeckNews.Data;

namespace TeckNews.Repositories
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : class
    {
        private readonly TeckNewsContext _dbContext;
        public DbSet<TEntity> Entities { get; }

        ///// for edite and read////
        public virtual IQueryable<TEntity> Table => Entities;
        /////for only read////
        public virtual IQueryable<TEntity> TableNoTracking => Entities.AsNoTracking();
        public BaseRepository(TeckNewsContext dbContext)
        {
            _dbContext = dbContext;
            Entities = _dbContext.Set<TEntity>();
        }

        #region Async Method
        public virtual ValueTask<TEntity> GetByIdAsync(CancellationToken cancellationToken, params object[] ids)
        {
            return Entities.FindAsync(ids, cancellationToken);
        }

        public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true)
        {
            var obj = (await Entities.AddAsync(entity, cancellationToken).ConfigureAwait(false)).Entity;
            if (saveNow)
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return obj;
        }

        public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
        {
            await Entities.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
            if (saveNow)
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true)
        {
            var obj = Entities.Update(entity).Entity;

            if (saveNow)
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return obj;
        }

        public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
        {
            Entities.UpdateRange(entities);

            if (saveNow)
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true)
        {
            Entities.Remove(entity);

            if (saveNow)
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
        {
            Entities.RemoveRange(entities);

            if (saveNow)
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region Sync Methods
        public virtual TEntity GetById(params object[] ids)
        {
            return Entities.Find(ids);
        }

        public virtual TEntity Add(TEntity entity, bool saveNow = true)
        {
            var obj = Entities.Add(entity).Entity;

            if (saveNow)
                _dbContext.SaveChanges();

            return obj;
        }

        public virtual void AddRange(IEnumerable<TEntity> entities, bool saveNow = true)
        {
            Entities.AddRange(entities);
            if (saveNow)
                _dbContext.SaveChanges();
        }

        public virtual TEntity Update(TEntity entity, bool saveNow = true)
        {
            var obj = Entities.Update(entity).Entity;
            if (saveNow)
                _dbContext.SaveChanges();

            return obj;
        }

        public virtual void UpdateRange(IEnumerable<TEntity> entities, bool saveNow = true)
        {
            Entities.UpdateRange(entities);
            if (saveNow)
                _dbContext.SaveChanges();
        }

        public virtual void Delete(TEntity entity, bool saveNow = true)
        {
            Entities.Remove(entity);
            if (saveNow)
                _dbContext.SaveChanges();
        }

        public virtual void DeleteRange(IEnumerable<TEntity> entities, bool saveNow = true)
        {
            Entities.RemoveRange(entities);
            if (saveNow)
                _dbContext.SaveChanges();
        }
        #endregion

        #region Attach & Detach
        public virtual void Detach(TEntity entity)
        {
            var entry = _dbContext.Entry(entity);
            if (entry != null)
                entry.State = EntityState.Detached;
        }

        public virtual void Attach(TEntity entity)
        {
            if (_dbContext.Entry(entity).State == EntityState.Detached)
                Entities.Attach(entity);
        }
        #endregion

    }
}
