

using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;
using Dobrasync.Core.Client.Main.Services.Logger;
using Dobrasync.Core.Client.Main.Services.Sync;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Clone;

public class CloneCommand(ILoggerService logger, IApiClient apiClient, ISyncService syncService) : ICommand
{
    public string GetName()
    {
        return "clone";
    }

    public async Task<int> Execute(string[] args)
    {
        var result = Parser.Default.ParseArguments<CloneOptions>(args);
        if (result.Errors.Any()) return 1;

        var code = await Clone(result);

        return 0;
    }

    private async Task<int> Clone(ParserResult<CloneOptions> results)
    {
        var libraryId = results.Value.LibraryId;
        var localLibraryPath = results.Value.LocalLibraryPath;

        await syncService.CloneLibrary(libraryId, localLibraryPath);

        return 0;
    }
}