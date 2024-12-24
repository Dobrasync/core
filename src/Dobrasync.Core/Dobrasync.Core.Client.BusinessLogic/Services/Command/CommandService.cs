using Dobrasync.Core.Client.Main.Const;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Clone;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Configure;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Create;
using Dobrasync.Core.Client.Main.Services.Command.Commands.List;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Login;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Logout;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Remove;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Scan;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Stats;
using Dobrasync.Core.Client.Main.Services.Logger;

namespace Dobrasync.Core.Client.Main.Services.Command;

public class CommandService(IServiceProvider serviceProvider, ILoggerService logger) : ICommandService
{
    private readonly List<ICommand> commands = new()
    {
        ActivatorUtilities.CreateInstance<LoginCommand>(serviceProvider),
        ActivatorUtilities.CreateInstance<LogoutCommand>(serviceProvider),
        ActivatorUtilities.CreateInstance<CloneCommand>(serviceProvider),
        ActivatorUtilities.CreateInstance<RemoveCommand>(serviceProvider),
        ActivatorUtilities.CreateInstance<ListCommand>(serviceProvider),
        ActivatorUtilities.CreateInstance<ConfigureCommand>(serviceProvider),
        ActivatorUtilities.CreateInstance<SyncCommand>(serviceProvider),
        ActivatorUtilities.CreateInstance<StatsCommand>(serviceProvider),
        ActivatorUtilities.CreateInstance<CreateCommand>(serviceProvider)
    };

    public async Task<int> Consume(string[] args)
    {
        if (args.Length == 0)
        {
            logger.LogFatal(
                $"No command provided. Valid commands are: {string.Join(", ", commands.Select(x => x.GetName()).ToArray())}");
            return ExitCodes.Failure;
        }

        var commandMatch =
            commands.FirstOrDefault(x => x.GetName().Equals(args[0], StringComparison.OrdinalIgnoreCase));
        if (commandMatch == null)
        {
            logger.LogFatal(
                $"Invalid command '{args[0]}'. Valid commands are: {string.Join(", ", commands.Select(x => x.GetName()).ToArray())}");
            return ExitCodes.Failure;
        }

        try
        {
            var exitCode = await commandMatch.Execute(args);
            return exitCode;
        }
        catch (Exception e)
        {
            logger.LogFatal($"Command failed: {e.Message}");
            logger.LogDebug($"Command stack-trace: {e.StackTrace}");
        }

        return ExitCodes.Failure;
    }
}