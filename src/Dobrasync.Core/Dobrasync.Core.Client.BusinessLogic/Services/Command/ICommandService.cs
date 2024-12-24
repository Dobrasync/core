

namespace Dobrasync.Core.Client.Main.Services.Command;

public interface ICommandService
{
    public Task<int> Consume(string[] args);
}