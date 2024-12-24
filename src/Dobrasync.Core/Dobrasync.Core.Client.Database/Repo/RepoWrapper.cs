using Dobrasync.Core.Client.Database.Entities;

namespace Dobrasync.Core.Client.Database.Repo;

public class RepoWrapper(LamashareContext context) : IRepoWrapper
{
    private IRepo<BlockEntity> _blockRepo = null!;
    private IRepo<FileEntity> _fileRepo = null!;
    private IRepo<LibraryEntity> _libraryRepo = null!;
    private IRepo<SystemSettingEntity> _systemSettingRepo = null!;

    public LamashareContext DbContext => context;

    #region Repos

    public IRepo<SystemSettingEntity> SystemSettingRepo
    {
        get { return _systemSettingRepo ??= new Repo<SystemSettingEntity>(context); }
    }

    public IRepo<LibraryEntity> LibraryRepo
    {
        get { return _libraryRepo ??= new Repo<LibraryEntity>(context); }
    }

    public IRepo<FileEntity> FileRepo
    {
        get { return _fileRepo ??= new Repo<FileEntity>(context); }
    }

    public IRepo<BlockEntity> BlockRepo
    {
        get { return _blockRepo ??= new Repo<BlockEntity>(context); }
    }

    #endregion
}