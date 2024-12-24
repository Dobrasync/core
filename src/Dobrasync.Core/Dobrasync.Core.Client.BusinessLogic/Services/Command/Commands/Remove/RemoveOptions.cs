

using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Remove;

[Verb("remove", HelpText = "Remove a library")]
public class RemoveOptions : BaseCommandOptions
{
    [Value(0, MetaName = "Action", Required = true)]
    public string Action { get; set; } = default!;

    [Value(1, MetaName = "Library name or id", HelpText = "Name or Id of the library to be removed", Required = true)]
    public Guid LibraryId { get; set; }

    [Option('l', "delete-local", Default = false, Required = false)]
    public bool RemoveLocalFiles { get; set; } = false;

    [Option('r', "delete-remote", Default = false, Required = false)]
    public bool RemoveRemoteFiles { get; set; } = false;
}