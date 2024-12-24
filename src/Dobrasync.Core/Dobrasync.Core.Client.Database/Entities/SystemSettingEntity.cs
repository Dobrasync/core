using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dobrasync.Core.Client.Database.Entities;

public class SystemSettingEntity
{
    [MaxLength(64)]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Id { get; set; } = default!;

    [MaxLength(1024)] public string? Value { get; set; }
}