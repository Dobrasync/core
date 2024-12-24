

using Dobrasync.Core.Client.Main.Const;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;
using Dobrasync.Core.Client.Main.Services.Logger;
using Dobrasync.Core.Client.Main.Services.Sync;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Scan;

public class SyncCommand(ISyncService syncService, ILoggerService logger) : ICommand
{
    public string GetName()
    {
        return "sync";
    }

    public async Task<int> Execute(string[] args)
    {
        var startTime = DateTimeOffset.UtcNow;
        var result = Parser.Default.ParseArguments<SyncOptions>(args);
        if (result.Errors.Any()) return 1;

        if (result.Value.SyncAll)
        {
            var c = await syncService.SyncAllLibraries();
            logger.LogInfo($"Sync took {(DateTimeOffset.UtcNow - startTime).TotalSeconds} seconds.");
            return c;
        }

        if (result.Value.LibraryId == null)
        {
            logger.LogError("Library id is required.");
            return ExitCodes.Failure;
        }

        var exitCode = await syncService.SyncLibrary(result.Value.LibraryId ?? new Guid());

        logger.LogInfo($"Sync took {(DateTimeOffset.UtcNow - startTime).TotalSeconds} seconds.");
        return exitCode;
    }
}