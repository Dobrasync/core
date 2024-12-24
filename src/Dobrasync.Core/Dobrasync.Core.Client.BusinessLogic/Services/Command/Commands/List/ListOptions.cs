

using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.List;

[Verb("list", HelpText = "List all local libraries")]
public class ListOptions : BaseCommandOptions
{
    [Value(0, MetaName = "Action", Required = true)]
    public string Action { get; set; } = default!;
}