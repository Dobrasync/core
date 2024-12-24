using Newtonsoft.Json;
using JsonException = Newtonsoft.Json.JsonException;


namespace Dobrasync.Core.Client.Main.Util;

public static class ApiUtil
{
    public static ErrorDto GetErrorDto(this ApiException apiException)
    {
        ErrorDto error = new()
        {
            Message = "Server did not return valid error",
            DateTimeUtc = DateTime.UtcNow,
            HttpStatusCode = 500
        };
        try
        {
            error = JsonConvert.DeserializeObject<ErrorDto>(apiException.Response) ?? error;
        }
        catch (JsonException e)
        {
            return error;
        }

        return error;
    }
}