using System.ComponentModel.DataAnnotations;
using Dobrasync.Core.Client.Database.Entities.Base;

namespace Dobrasync.Core.Client.Database.Entities;

public class LibraryEntity : BaseEntity
{
    /// <summary>
    ///     Path to the directory in which the library is stored locally.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(4096)]
    public string LocalPath { get; set; } = default!;

    [Required] public Guid RemoteId { get; set; }

    public List<FileEntity> Files { get; set; } = new();
}