namespace LamashareCore.Models;

public class Block
{
    public byte[] Payload { get; set; } = default!;
    public string Checksum { get; set; } = default!;
    public long Offset { get; set; } = default!;
}