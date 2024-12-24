using Dobrasync.Core.Client.Database.Entities;

namespace Dobrasync.Core.Client.Database.Repo;

public interface IRepoWrapper
{
    LamashareContext DbContext { get; }
    IRepo<SystemSettingEntity> SystemSettingRepo { get; }
    IRepo<LibraryEntity> LibraryRepo { get; }
    IRepo<FileEntity> FileRepo { get; }
    IRepo<BlockEntity> BlockRepo { get; }
}