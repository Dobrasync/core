

namespace Dobrasync.Core.Client.Main.Services.Logger;

public class LoggerService(ILogger<LoggerService> logger) : ILoggerService
{
    public void LogDebug(string msg)
    {
        logger.LogDebug(msg);
    }

    public void LogInfo(string msg)
    {
        logger.LogInformation(msg);
    }

    public void LogWarn(string msg)
    {
        logger.LogWarning(msg);
    }

    public void LogError(string msg)
    {
        logger.LogError(msg);
    }

    public void LogFatal(string msg)
    {
        logger.LogCritical(msg);
    }
}