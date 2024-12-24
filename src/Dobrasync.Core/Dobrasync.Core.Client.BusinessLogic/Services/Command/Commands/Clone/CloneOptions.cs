

using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Clone;

[Verb("clone", HelpText = "Clone a library")]
public class CloneOptions : BaseCommandOptions
{
    [Value(0, MetaName = "Action", Required = true)]
    public string Action { get; set; } = default!;

    [Value(1, MetaName = "Library Id", HelpText = "Id of the library to be cloned", Required = true)]
    public Guid LibraryId { get; set; }

    [Value(2, MetaName = "Library path",
        HelpText =
            "Where to put the local copy of the library on your system. If not provided, a new library will be created in the default library location.",
        Required = false)]
    public string LocalLibraryPath { get; set; } = default!;
}