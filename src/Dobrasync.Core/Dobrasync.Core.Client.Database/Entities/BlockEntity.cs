using Dobrasync.Core.Client.Database.Entities.Base;

namespace Dobrasync.Core.Client.Database.Entities;

public class BlockEntity : BaseEntity
{
    public string Checksum { get; set; } = default!;
    public HashSet<FileEntity> Files { get; set; } = new();
    public long Offset { get; set; }
    public int Size { get; set; }
    public LibraryEntity Library { get; set; } = default!;
}