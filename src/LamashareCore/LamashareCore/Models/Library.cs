namespace LamashareCore.Models;

public class Library
{
    public Guid Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string LocalLibraryPath { get; set; } = default!;
}