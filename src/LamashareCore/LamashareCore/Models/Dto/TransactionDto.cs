namespace LamashareCore.Models;

public class TransactionDto
{
    public Guid TransactionId { get; set; } = default!;
    public bool IsComplete { get; set; } = default!;
}