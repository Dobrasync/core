

using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;
using Dobrasync.Core.Client.Main.Services.Sync;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Remove;

public class RemoveCommand(ISyncService syncService) : ICommand
{
    public string GetName()
    {
        return "remove";
    }

    public async Task<int> Execute(string[] args)
    {
        var result = Parser.Default.ParseArguments<RemoveOptions>(args);
        if (result.Errors.Any()) return 1;

        var code = await syncService.RemoveLibrary(result.Value.LibraryId, result.Value.RemoveLocalFiles,
            result.Value.RemoveRemoteFiles);
        return code;
    }
}