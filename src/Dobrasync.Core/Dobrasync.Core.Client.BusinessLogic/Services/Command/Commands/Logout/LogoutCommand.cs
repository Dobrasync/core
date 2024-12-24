

using Dobrasync.Core.Client.Main.Const;
using Dobrasync.Core.Client.Main.Services.Auth;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Login;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Logout;

public class LogoutCommand(IAuthService authService) : ICommand
{
    public string GetName()
    {
        return "logout";
    }

    public async Task<int> Execute(string[] args)
    {
        var result = Parser.Default.ParseArguments<LoginOptions>(args);
        if (result.Errors.Any()) return ExitCodes.Failure;

        await authService.LogoutAsync();

        return ExitCodes.Success;
    }
}