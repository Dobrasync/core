

using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Logout;

[Verb("logout", HelpText = "Logout operation")]
public class LogoutOptions : BaseCommandOptions
{
    [Value(0, MetaName = "Action", Required = true)]
    public string Action { get; set; }
}