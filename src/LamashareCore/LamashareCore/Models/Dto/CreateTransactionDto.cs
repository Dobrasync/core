namespace LamashareCore.Models;

public class CreateTransactionDto
{
    public string OriginalFileName { get; set; } = default!;
    public long OriginalFileSize { get; set; } = default!;
    public long BlockCount { get; set; } = default!;
    public string TotalChecksum { get; set; } = default!;
}