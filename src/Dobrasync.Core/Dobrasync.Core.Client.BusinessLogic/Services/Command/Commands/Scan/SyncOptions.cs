

using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Scan;

[Verb("sync", HelpText = "Sync a local library with remote.")]
public class SyncOptions : BaseCommandOptions
{
    [Value(0, MetaName = "Action", Required = true)]
    public string Action { get; set; } = default!;

    [Value(1, MetaName = "Library Local-ID", HelpText = "Local-Id of the library to sync", Required = false)]
    public Guid? LibraryId { get; set; }

    [Option('a', "all", Default = false, Required = false)]
    public bool SyncAll { get; set; } = false;
}