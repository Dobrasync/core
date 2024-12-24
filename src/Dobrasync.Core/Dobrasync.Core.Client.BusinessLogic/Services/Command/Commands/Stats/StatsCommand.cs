using System.Text;
using Dobrasync.Core.Client.Database.Repo;
using Dobrasync.Core.Client.Main.Const;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;
using Dobrasync.Core.Client.Main.Services.Logger;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Stats;

public class StatsCommand(ILoggerService logger, IRepoWrapper repoWrap) : ICommand
{
    public string GetName()
    {
        return "stats";
    }

    public async Task<int> Execute(string[] args)
    {
        logger.LogInfo("Collecting stats...");

        var blockCount = await repoWrap.BlockRepo.QueryAll().CountAsync();
        var storageUsage = 0;
        if (blockCount > 0)
            storageUsage =
                (await repoWrap.BlockRepo.QueryAll().Select(x => x.Size).ToListAsync()).Aggregate((x, y) => x + y);

        StringBuilder sb = new();

        sb.AppendLine("Stats:");
        sb.AppendLine($"- Block count: {blockCount}");
        sb.AppendLine($"- Storage usage: {storageUsage / 1000} kb");

        logger.LogInfo(sb.ToString());

        return ExitCodes.Success;
    }
}