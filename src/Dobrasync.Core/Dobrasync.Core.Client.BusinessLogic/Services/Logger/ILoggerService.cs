

namespace Dobrasync.Core.Client.Main.Services.Logger;

public interface ILoggerService
{
    public void LogDebug(string msg);

    public void LogInfo(string msg);

    public void LogWarn(string msg);

    public void LogError(string msg);

    public void LogFatal(string msg);
}