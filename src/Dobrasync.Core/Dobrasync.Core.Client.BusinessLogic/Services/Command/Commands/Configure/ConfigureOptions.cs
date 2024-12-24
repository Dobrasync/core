

using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Configure;

[Verb("clone", HelpText = "Clone a library")]
public class ConfigureOptions : BaseCommandOptions
{
    [Value(0, MetaName = "Action", Required = true)]
    public string Action { get; set; } = default!;

    [Option('d', "library-dir", Required = false, HelpText = "Set the default directory for new libraries.")]
    public string? DefaultDirectory { get; set; }

    [Option('r', "remote", Required = false, HelpText = "Set the default remote.")]
    public string? DefaultRemote { get; set; }

    [Option('b', "temp-block-dir", Required = false, HelpText = "Set the temp block directory.")]
    public string? TempBlockDir { get; set; }

    [Option('l', "list", Required = false, Default = false, HelpText = "Lists all system setting values.")]
    public bool List { get; set; }
}