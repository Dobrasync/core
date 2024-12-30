using System.Text.Json;
using Dobrasync.Core.Client.ApiGen.Mainline;
using Dobrasync.Core.Client.BusinessLogic.Services.Auth;
using Dobrasync.Core.Client.BusinessLogic.Services.Block;
using Dobrasync.Core.Client.BusinessLogic.Services.Logger;
using Dobrasync.Core.Client.BusinessLogic.Services.SystemSetting;
using Dobrasync.Core.Client.Common.Helpers;
using Dobrasync.Core.Client.Database.Entities;
using Dobrasync.Core.Client.Database.Enums;
using Dobrasync.Core.Client.Database.Repo;
using Dobrasync.Core.Common.Util;
using Microsoft.EntityFrameworkCore;

namespace Dobrasync.Core.Client.BusinessLogic.Services.Sync;

public class SyncService(
    IApiClient apiClient,
    IRepoWrapper repoWrap,
    ILoggerService logger,
    ISystemSettingService settings,
    IBlockService blockService,
    IAuthService authService) : ISyncService
{
    #region Create

    public async Task<int> CreateLibrary(string name)
    {
        await authService.RequireLoggedInAsync();

        LibraryDto createdLibrary;
        try
        {
            createdLibrary = await apiClient.CreateLibraryAsync(new LibraryCreateDto
            {
                Name = name
            });
        }
        catch (ApiException e)
        {
            var error = e.GetErrorDto();
            logger.LogFatal($"Failed to create library on remote: {error.Message}");
            logger.LogDebug($"Stack trace: {e.StackTrace}");
            return ExitCodes.Failure;
        }

        logger.LogInfo(
            $"Library '{createdLibrary.Name}' has been created successfully on remote (Remote-ID: '{createdLibrary.Id}').");

        return ExitCodes.Success;
    }

    #endregion

    #region Clone

    public async Task<int> CloneLibrary(Guid remoteLibraryId, string? localLibraryPath)
    {
        await authService.RequireLoggedInAsync();

        #region Check if already cloned

        var alreadyCloned = await IsLibraryCloned(remoteLibraryId);
        if (alreadyCloned)
        {
            logger.LogError("Library has already been cloned.");
            return 1;
        }

        #endregion

        #region Load required data

        LibraryDto? remoteLibrary = null;
        try
        {
            remoteLibrary = await apiClient.GetLibraryByIdAsync(remoteLibraryId);
        }
        catch (ApiException e)
        {
            logger.LogError(e.Message);
            return 1;
        }

        if (remoteLibrary == null)
        {
            logger.LogError("Library with given id does not exist.");
            return 1;
        }

        #endregion

        #region Create library directory in default dir

        if (localLibraryPath == null)
        {
            var def = await settings.TryGetSettingAsync(ESystemSetting.DEFAULT_LIBRARY_DIRECTORY);
            if (def == null)
            {
                logger.LogError("Default library path is not configured.");
                return 1;
            }

            if (string.IsNullOrEmpty(def.Value))
            {
                logger.LogError($"The default library path is invalid: '{def.Value}'");
                return 1;
            }

            localLibraryPath = Path.Join(def.Value, remoteLibrary.Name);
            try
            {
                Directory.CreateDirectory(localLibraryPath);
            }
            catch (Exception e)
            {
                logger.LogError($"Could not create a new directory for library: {e.Message}");
                logger.LogDebug($"Stack trace: {e.StackTrace}");
                return 1;
            }
        }

        ;

        #endregion

        #region Throw if locallibrarypath does not exist

        if (!Directory.Exists(localLibraryPath))
        {
            logger.LogError("Local library path does not exist or is not a directory.");
            return 1;
        }

        #endregion

        #region Create db entry

        var localLib = new LibraryEntity
        {
            LocalPath = localLibraryPath,
            RemoteId = remoteLibraryId
        };
        await repoWrap.LibraryRepo.InsertAsync(localLib);

        #endregion

        logger.LogInfo(
            $"Library has been cloned (Local-ID: '{localLib.Id}', Local-Directory: '{localLib.LocalPath}').");
        return 0;
    }

    #endregion

    #region Remove

    public async Task<int> RemoveLibrary(Guid localLibraryId, bool deleteDirectory = false,
        bool deleteRemoteLibrary = false)
    {
        if (deleteRemoteLibrary) await authService.RequireLoggedInAsync();

        #region Check if library is cloned

        var lib = await repoWrap.LibraryRepo.GetByIdAsync(localLibraryId);
        if (lib == null)
        {
            logger.LogError("Library with given id does not exist.");
            return ExitCodes.Failure;
        }

        #endregion

        #region Remove from DB

        await repoWrap.LibraryRepo.DeleteAsync(lib);
        logger.LogInfo("Library has been removed.");

        #endregion

        #region Delete local files

        if (deleteDirectory && !string.IsNullOrEmpty(lib.LocalPath))
        {
            Directory.Delete(lib.LocalPath, true);
            logger.LogInfo("Library directory deleted.");
        }

        #endregion

        #region Delete remote library

        if (deleteRemoteLibrary)
        {
            await apiClient.DeleteLibraryByIdAsync(lib.RemoteId);
            logger.LogInfo("Library deleted from remote.");
        }

        #endregion

        return ExitCodes.Success;
    }

    #endregion

    #region Sync all

    public async Task<int> SyncAllLibraries()
    {
        await authService.RequireLoggedInAsync();

        var libs = await repoWrap.LibraryRepo.QueryAll().ToListAsync();
        if (libs.Count == 0)
        {
            logger.LogInfo("No libraries to sync.");
            return ExitCodes.Success;
        }

        foreach (var lib in libs) await SyncLibrary(lib.Id);
        return ExitCodes.Success;
    }

    #endregion

    #region Sync

    public async Task<int> SyncLibrary(Guid localLibId)
    {
        await authService.RequireLoggedInAsync();

        #region load local library

        logger.LogInfo($"Invoking sync for {localLibId}...");
        var lib = await repoWrap.LibraryRepo.GetByIdAsync(localLibId);
        if (lib == null)
        {
            logger.LogError("Library with given id does not exist.");
            return ExitCodes.Failure;
        }

        #endregion

        #region Get diff

        List<FileEntity> deletedFiles = new();
        var diff = await Diff(lib.Id);

        #region Remove deleted files from diff

        foreach (var requiredByLocal in diff.RequiredByLocal)
        {
            var dbFile = repoWrap.FileRepo.QueryAll()
                .FirstOrDefault(x => x.LibraryEntity == lib && x.FileLibraryPath == requiredByLocal);
            if (dbFile == null) continue;

            // If the file exists in DB but not on file system, we assume it's been deleted.
            deletedFiles.Add(dbFile);
        }

        #endregion

        #endregion

        #region Pull

        List<string> actuallyRequiredByLocal = diff.RequiredByLocal
            .Where(x => deletedFiles.All(y => y.FileLibraryPath != x))
            .ToList();
        if (actuallyRequiredByLocal.Any())
        {
            logger.LogInfo($"Pulling {diff.RequiredByLocal.Count()} out-of-sync files from remote...");
            foreach (var file in diff.RequiredByLocal) await PullFile(lib.Id, file);
        }
        else
        {
            logger.LogInfo("No new or newer files on remote, skipping pull.");
        }

        #endregion

        #region Push

        if (diff.RequiredByRemote.Any())
        {
            logger.LogInfo($"Pushing {diff.RequiredByRemote.Count()} out-of-sync local files to remote...");
            foreach (var file in diff.RequiredByRemote) await PushFile(lib.Id, file);
        }
        else
        {
            logger.LogInfo("No new or newer files on local, skipping push.");
        }

        #endregion

        #region Delete

        if (deletedFiles.Any())
        {
            logger.LogInfo($"Detected {deletedFiles.Count()} deleted files, removing them from remote...");
            foreach (var fileToDelete in deletedFiles) await DeleteFile(lib.Id, fileToDelete.FileLibraryPath);
        }
        else
        {
            logger.LogInfo("No file deletions found, skipping deletion on remote.");
        }

        #endregion

        logger.LogInfo($"Library {lib.Id} is in sync.");
        return 0;
    }

    #endregion

    #region Pull file

    private async Task<int> PullFile(Guid localLibId, string fileLocalPath)
    {
        await authService.RequireLoggedInAsync();

        #region load file

        logger.LogInfo($"Pulling file '{fileLocalPath}'...");
        var lib = await repoWrap.LibraryRepo.GetByIdAsyncThrows(localLibId);
        var file = fileLocalPath;

        #endregion

        #region Transaction - START

        var transaction = await apiClient.CreateFileTransactionAsync(new FileTransactionCreateDto
        {
            LibraryId = lib.RemoteId,
            FileLibraryPath = file,
            Type = EFileTransactionType.PULL
        });
        logger.LogDebug($"Began pull transaction with result: {JsonSerializer.Serialize(transaction)}");

        #endregion

        #region Fetch remote file info

        var remoteFileInfo = await apiClient.GetFileInfoAsync(lib.RemoteId, file);
        var remoteFileBlocklist = await apiClient.GetFileBlockListAsync(lib.RemoteId, file);

        #endregion

        #region Pull blocks required by local

        // TODO: Build diff, for testing we just full everything for now
        var index = 0;
        foreach (var block in remoteFileBlocklist)
        {
            logger.LogInfo($"Pulling block {index + 1} of {remoteFileBlocklist.Count}...");
            var pulled = await apiClient.PullBlockAsync(block);
            await blockService.WriteTempBlock(pulled.Checksum, pulled.Content);
            index++;
        }

        #endregion

        #region Transaction - FINISH

        var result = await apiClient.FinishFileTransactionAsync(transaction.Id);
        logger.LogDebug($"Finished pull transaction with result: {JsonSerializer.Serialize(result)}");

        #endregion

        #region Restore file from blocks

        await blockService.RestoreFileFromBlocks(
            remoteFileBlocklist.ToList(),
            FileUtil.FileLibPathToSysPath(lib.LocalPath, remoteFileInfo.FileLibraryPath),
            remoteFileInfo.DateModified.UtcDateTime,
            remoteFileInfo.DateCreated.UtcDateTime
        );

        #endregion

        return ExitCodes.Success;
    }

    #endregion

    #region Push file

    private async Task<int> PushFile(Guid localLibId, string fileLocalPath)
    {
        await authService.RequireLoggedInAsync();

        #region load

        logger.LogInfo($"Pushing file '{fileLocalPath}'...");
        var lib = await repoWrap.LibraryRepo.GetByIdAsyncThrows(localLibId);
        var file = fileLocalPath;

        #endregion

        #region Get local file info and data

        var fileSysPath = FileUtil.FileLibPathToSysPath(lib.LocalPath, file);
        var localFileInfo = new FileInfo(fileSysPath);
        var localFileBlocklist = FileUtil.GetFileBlocks(fileSysPath);
        var localFileTotalChecksum = await FileUtil.GetFileTotalChecksumAsync(fileSysPath);

        #endregion

        #region Begin transaction

        var transaction = await apiClient.CreateFileTransactionAsync(new FileTransactionCreateDto
        {
            LibraryId = lib.RemoteId,
            FileLibraryPath = file,
            Type = EFileTransactionType.PUSH,
            BlockChecksums = localFileBlocklist.Select(x => x.Checksum).ToArray(),
            TotalChecksum = localFileTotalChecksum,
            DateModifiedFile = localFileInfo.LastWriteTimeUtc,
            DateCreatedFile = localFileInfo.CreationTimeUtc
        });
        logger.LogDebug($"Began transaction with result: {JsonSerializer.Serialize(transaction)}");

        #endregion

        #region Push required blocks

        long index = 0;
        foreach (var remoteBlock in transaction.RequiredBlocks)
        {
            logger.LogInfo($"Pushing block {index + 1} of {transaction.RequiredBlocks.Count}.");
            var block = localFileBlocklist.FirstOrDefault(x => x.Checksum == remoteBlock);
            if (block == null) throw new ArgumentException("Invalid block");

            var pushResult = await apiClient.PushBlockAsync(new BlockPushDto
            {
                Checksum = block.Checksum,
                Content = block.Payload,
                TransactionId = transaction.Id,
                LibraryId = lib.RemoteId,
                Offset = block.Offset,
                Size = block.Payload.Length
            });
            logger.LogDebug($"Pushed block '{block.Checksum}'.");
            index++;
        }

        #endregion

        #region finalize transaction

        var result = await apiClient.FinishFileTransactionAsync(transaction.Id);
        logger.LogDebug(
            $"Push of file '${localFileInfo.Name}' finish with result: ${JsonSerializer.Serialize(result)}");

        #endregion

        return ExitCodes.Success;
    }

    #endregion

    #region Delete file

    private async Task<int> DeleteFile(Guid localLibId, string fileLocalPath)
    {
        await authService.RequireLoggedInAsync();

        #region load

        var lib = await repoWrap.LibraryRepo.GetByIdAsyncThrows(localLibId);

        #endregion

        #region Delete file from remote

        try
        {
            await apiClient.DeleteFileAsync(lib.RemoteId, fileLocalPath);
        }
        catch (ApiException e)
        {
            if (e.StatusCode != 404) throw;

            logger.LogDebug($"Failed to delete file '{fileLocalPath}' from remote, it wasn't found.");
        }

        logger.LogInfo($"Deleted file {fileLocalPath} from remote.");

        #endregion

        #region Delete file from local db

        var file = await repoWrap.FileRepo
            .QueryAll()
            .Include(x => x.LibraryEntity)
            .Include(x => x.Blocks)
            .ThenInclude(x => x.Files)
            .FirstOrDefaultAsync(x => x.LibraryEntity == lib && x.FileLibraryPath == fileLocalPath);

        if (file == null) return ExitCodes.Success;

        #region Remove blocks from local db that arent present on other files

        List<BlockEntity> blocksToRemove = file.Blocks
            .Where(x => x.Files.Count() <= 1)
            .ToList();
        await repoWrap.BlockRepo.DeleteRangeAsync(blocksToRemove);

        #endregion

        #region Update remaining blocks file list

        List<BlockEntity> blocksToUpdate = file.Blocks
            .Where(x => x.Files.Count() > 1)
            .ToList();

        blocksToUpdate.ForEach(x => x.Files.Remove(file));
        await repoWrap.BlockRepo.UpdateRangeAsync(blocksToUpdate);

        #endregion

        await repoWrap.FileRepo.DeleteAsync(file);

        #endregion

        return ExitCodes.Success;
    }

    #endregion

    #region Diff

    private async Task<LibraryDiffDto> Diff(Guid localLibId)
    {
        await authService.RequireLoggedInAsync();

        #region load library and its files

        var lib = await repoWrap.LibraryRepo.GetByIdAsyncThrows(localLibId);
        List<FileEntity> libraryFiles = await GetLocalLibraryFileTree(lib.Id);

        #endregion

        #region Get diff list from remote

        logger.LogInfo("Getting diff from remote...");
        var diff = await apiClient.GetDiffAsync(new CreateDiffDto
        {
            LibraryId = lib.RemoteId,
            FilesOnLocal = libraryFiles.Select(x => new FileInfoDto
            {
                LibraryId = lib.RemoteId,
                FileLibraryPath = x.FileLibraryPath,
                TotalChecksum = x.TotalChecksum,
                DateModified = x.DateModified,
                DateCreated = x.DateCreated
            }).ToList()
        });

        #endregion

        return diff;
    }

    #endregion

    #region Get library file tree

    private async Task<List<FileEntity>> GetLocalLibraryFileTree(Guid localLibId)
    {
        #region Load library

        var lib = await repoWrap.LibraryRepo.GetByIdAsyncThrows(localLibId);

        #endregion

        #region Re-Generate local file tree and update db

        logger.LogInfo("Getting local library file tree...");
        var localFiles = Directory.GetFiles(lib.LocalPath, "*.*", SearchOption.AllDirectories);
        List<FileEntity> libraryFiles = new();
        foreach (var syspath in localFiles)
        {
            #region Get file info from file system

            FileInfo info = new(syspath);
            var fileLibPath = FileUtil.FileSysPathToLibPath(syspath, lib.LocalPath);
            var fileTotalChecksum = await FileUtil.GetFileTotalChecksumAsync(syspath);
            DateTimeOffset dateModified = info.LastWriteTimeUtc;
            DateTimeOffset dateCreated = info.CreationTimeUtc;

            #endregion

            #region Update local db (add or update file)

            #region Load existing file if exists

            var existingDbFile = await repoWrap.FileRepo
                .QueryAll()
                .Include(x => x.LibraryEntity)
                .Include(x => x.Blocks)
                .ThenInclude(x => x.Files)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.LibraryEntity.Id == localLibId && x.FileLibraryPath == fileLibPath);

            #endregion

            #region Create or update existing db file

            if (existingDbFile != null)
            {
                // Regenerate block index if the total checksum has changed
                // TODO: Optimization - Introduce a ContentChecksum that excludes
                // metadata changes so we dont have to regenerate blocks
                // even if its just the metadata that has changed.
                if (fileTotalChecksum != existingDbFile.TotalChecksum)
                {
                    var newBlocks = FileUtil.GetFileBlocks(syspath);

                    List<BlockEntity> newBlockAssembly = new();
                    foreach (var newBlock in newBlocks)
                    {
                        var existingBlock = await repoWrap.BlockRepo
                            .QueryAll()
                            .FirstOrDefaultAsync(x => x.Checksum == newBlock.Checksum);

                        if (existingBlock != null)
                        {
                            newBlockAssembly.Add(existingBlock);
                        }
                        else
                        {
                            existingBlock = new BlockEntity
                            {
                                Checksum = newBlock.Checksum,
                                Files = [existingDbFile],
                                Offset = newBlock.Offset,
                                Size = newBlock.Payload.Length,
                                Library = existingDbFile.LibraryEntity
                            };
                            await repoWrap.BlockRepo.InsertAsync(existingBlock);
                            newBlockAssembly.Add(existingBlock);
                        }
                    }

                    existingDbFile.Blocks = newBlockAssembly;
                }

                existingDbFile.TotalChecksum = fileTotalChecksum;
                existingDbFile.DateCreated = dateCreated;
                existingDbFile.DateModified = dateModified;

                await repoWrap.FileRepo.UpdateAsync(existingDbFile);
                await DeleteOrphanedBlocks();
                libraryFiles.Add(existingDbFile);
            }
            else
            {
                FileEntity newFileEntity = new()
                {
                    FileLibraryPath = fileLibPath,
                    TotalChecksum = fileTotalChecksum,
                    LibraryEntity = lib,
                    DateCreated = dateCreated,
                    DateModified = dateModified
                };

                await repoWrap.FileRepo.InsertAsync(newFileEntity);
                libraryFiles.Add(newFileEntity);
            }

            #endregion

            #endregion
        }

        #endregion

        return libraryFiles;
    }

    #endregion

    #region Utility

    private async Task DeleteOrphanedBlocks()
    {
        List<BlockEntity> toDelete = await repoWrap.BlockRepo.QueryAll().Where(x => !x.Files.Any()).ToListAsync();
        if (!toDelete.Any()) return;

        logger.LogInfo($"Deleting {toDelete.Count()} orphaned blocks...");
        await repoWrap.BlockRepo.DeleteRangeAsync(toDelete);
    }

    private async Task<bool> IsLibraryCloned(Guid remoteLibraryId)
    {
        var match = await repoWrap.LibraryRepo.QueryAll().FirstOrDefaultAsync(x => x.RemoteId == remoteLibraryId);
        return match != null;
    }

    #endregion
}