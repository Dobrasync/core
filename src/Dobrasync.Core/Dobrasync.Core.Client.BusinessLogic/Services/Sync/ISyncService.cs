namespace Dobrasync.Core.Client.BusinessLogic.Services.Sync;

public interface ISyncService
{
    /// <summary>
    ///     Create a new library on remote.
    /// </summary>
    /// <param name="name">Display-name of the new library</param>
    /// <returns></returns>
    public Task<int> CreateLibrary(string name);

    /// <summary>
    ///     Clones a library on remote.
    /// </summary>
    /// <param name="libraryId">Remote-ID of the library to clone</param>
    /// <param name="localLibraryPath">Where to clone the library to</param>
    /// <returns></returns>
    public Task<int> CloneLibrary(Guid libraryId, string localLibraryPath);

    /// <summary>
    ///     Removes a sync from local.
    /// </summary>
    /// <param name="libraryId">Local library id</param>
    /// <param name="deleteDirectory">If the local directory should be deleted</param>
    /// <returns></returns>
    public Task<int> RemoveLibrary(Guid libraryId, bool deleteDirectory, bool deleteRemoteDirectory);

    /// <summary>
    ///     Synchronizes the given library with remote.
    /// </summary>
    /// <param name="localLibId">Local ID of library to sync</param>
    /// <returns></returns>
    public Task<int> SyncLibrary(Guid localLibId);

    /// <summary>
    ///     Synchronizes all libraries on local machine with remote counterparts.
    /// </summary>
    /// <returns></returns>
    public Task<int> SyncAllLibraries();
}