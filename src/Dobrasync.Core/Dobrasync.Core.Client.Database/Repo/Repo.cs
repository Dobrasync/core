using Dobrasync.Core.Client.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Dobrasync.Core.Client.Database.Repo;

public class Repo<TEntity> : IRepo<TEntity> where TEntity : class
{
    #region ctor

    public Repo(LamashareContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    #endregion

    public async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<TEntity> GetByIdAsyncThrows(Guid id)
    {
        var t = await GetByIdAsync(id);
        if (t == null) throw new EntityNotFoundException();
        return t;
    }

    public IQueryable<TEntity> QueryAll()
    {
        return _dbSet;
    }

    public async Task<TEntity> InsertAsync(TEntity entityToInsert)
    {
        _dbSet.Add(entityToInsert);
        await _context.SaveChangesAsync();

        return entityToInsert;
    }

    public async Task<List<TEntity>> InsertRangeAsync(List<TEntity> entitiesToInsert)
    {
        await _dbSet.AddRangeAsync(entitiesToInsert);
        await _context.SaveChangesAsync();

        return entitiesToInsert;
    }

    public async Task<TEntity> UpdateAsync(TEntity entityToUpdate)
    {
        _dbSet.Update(entityToUpdate);
        await _context.SaveChangesAsync();

        return entityToUpdate;
    }

    public async Task<List<TEntity>> UpdateRangeAsync(List<TEntity> entityToUpdate)
    {
        _dbSet.UpdateRange(entityToUpdate);
        await _context.SaveChangesAsync();

        return entityToUpdate;
    }

    public async Task<TEntity> DeleteAsync(TEntity entityToDelete)
    {
        _dbSet.Remove(entityToDelete);
        await _context.SaveChangesAsync();

        return entityToDelete;
    }

    public async Task<List<TEntity>> DeleteRangeAsync(List<TEntity> entitiesToDelete)
    {
        _dbSet.RemoveRange(entitiesToDelete);
        await _context.SaveChangesAsync();

        return entitiesToDelete;
    }

    #region fields

    private readonly LamashareContext _context;
    private readonly DbSet<TEntity> _dbSet;

    #endregion
}