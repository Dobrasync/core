using Dobrasync.Core.Client.ApiGen.Mainline;
using Dobrasync.Core.Common.Models;
using Newtonsoft.Json;
using JsonException = Newtonsoft.Json.JsonException;


namespace Dobrasync.Core.Client.Common.Helpers;

public static class ApiUtil
{
    public static ApiErrorDto GetErrorDto(this ApiException apiException)
    {
        ApiErrorDto error = new()
        {
            Message = "Server did not return valid error",
            DateTimeUtc = DateTime.UtcNow,
            HttpStatusCode = 500
        };
        try
        {
            error = JsonConvert.DeserializeObject<ApiErrorDto>(apiException.Response) ?? error;
        }
        catch (JsonException e)
        {
            return error;
        }

        return error;
    }
}