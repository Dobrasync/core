

using Dobrasync.Core.Client.Main.Const;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;
using Dobrasync.Core.Client.Main.Services.Sync;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Create;

public class CreateCommand(ISyncService syncService) : ICommand
{
    public string GetName()
    {
        return "create";
    }

    public async Task<int> Execute(string[] args)
    {
        var result = Parser.Default.ParseArguments<CreateOptions>(args);
        if (result.Errors.Any()) return ExitCodes.Failure;

        return await syncService.CreateLibrary(result.Value.Name);
    }
}