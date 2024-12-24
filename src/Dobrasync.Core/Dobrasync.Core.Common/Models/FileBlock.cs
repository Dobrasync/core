

namespace Dobrasync.Core.Common.Models;

public class FileBlock
{
    public byte[] Payload { get; set; } = default!;
    public string Checksum { get; set; } = default!;
    public long Offset { get; set; } = default!;
}