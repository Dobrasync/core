

using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Create;

[Verb("create", HelpText = "Create a new library")]
public class CreateOptions : BaseCommandOptions
{
    [Value(0, MetaName = "Action", Required = true)]
    public string Action { get; set; } = default!;

    [Value(1, MetaName = "Name", Required = true)]
    public string Name { get; set; } = default!;
}