

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Base;

public interface ICommand
{
    public string GetName();
    public Task<int> Execute(string[] args);
}