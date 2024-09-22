namespace LamashareCore.Models;

public class Block
{
    public Guid BlockId { get; set; } = default!;
    public byte[] Payload { get; set; } = default!;
    public string Checksum { get; set; } = default!;
}