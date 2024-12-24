

using Dobrasync.Core.Client.Database.Repo;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;
using Dobrasync.Core.Client.Main.Services.Logger;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.List;

public class ListCommand(IRepoWrapper repoWrap, ILoggerService logger, IApiClient apiClient) : ICommand
{
    public string GetName()
    {
        return "ls";
    }

    public async Task<int> Execute(string[] args)
    {
        var result = Parser.Default.ParseArguments<ListOptions>(args);
        if (result.Errors.Any()) return 1;

        var sessionInfo = await apiClient.GetSessionInfoAsync();

        logger.LogDebug("Loading list...");
        var localLibraries = await repoWrap.LibraryRepo.QueryAll().ToListAsync();
        var remoteLibraries = await apiClient.LibrariesAsync(sessionInfo.User.Id, 1, 100, null, null);

        logger.LogInfo($"Local libraries: {JsonSerializer.Serialize(localLibraries)}");
        logger.LogInfo($"Remote libraries: {JsonSerializer.Serialize(remoteLibraries.Data)}");
        return 0;
    }
}