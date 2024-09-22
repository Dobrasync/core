namespace LamashareCore.Models;

public class BlockDto
{
    public Guid TransactionId { get; set; }
    public byte[]? Content { get; set; } = default!;
    public string Checksum { get; set; } = default!;
    public long Index { get; set; }
}