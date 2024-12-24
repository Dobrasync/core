


namespace Dobrasync.Core.Client.Database;

public class GuidUtil
{
    public static Guid ToGuid(int value)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return new Guid(bytes);
    }
}