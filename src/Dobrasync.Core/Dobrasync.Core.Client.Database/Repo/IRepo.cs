namespace Dobrasync.Core.Client.Database.Repo;

public interface IRepo<TEntity>
{
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<TEntity> GetByIdAsyncThrows(Guid id);
    IQueryable<TEntity> QueryAll();
    Task<TEntity> InsertAsync(TEntity entityToInsert);
    Task<List<TEntity>> InsertRangeAsync(List<TEntity> entitiesToInsert);
    Task<TEntity> UpdateAsync(TEntity entityToUpdate);
    Task<List<TEntity>> UpdateRangeAsync(List<TEntity> entityToUpdate);
    Task<TEntity> DeleteAsync(TEntity entityToDelete);
    Task<List<TEntity>> DeleteRangeAsync(List<TEntity> entitiesToDelete);
}