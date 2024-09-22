namespace LamashareCore.Models;

public class FileDto
{
    public string LibraryId { get; set; } = default!;
    public string TotalChecksum { get; set; } = default!;
    public string FileLibraryPath { get; set; } = default!;
    public bool Locked { get; set; } = default!;
    public DateTime ModifiedOn { get; set; } = default!;
}