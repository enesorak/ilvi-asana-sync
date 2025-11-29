using System.ComponentModel.DataAnnotations.Schema;

namespace Ilvi.Asana.Domain.Entities;

/// <summary>
/// Asana projesi (Project + ProjectDetails birleştirildi)
/// </summary>
public class Project : BaseEntity
{
    public long WorkspaceId { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string Name { get; set; } = null!;

    public bool Archived { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string? Color { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Asana'daki oluşturulma tarihi
    /// </summary>
    public DateTime? AsanaCreatedAt { get; set; }

    /// <summary>
    /// Asana'daki son güncelleme tarihi
    /// </summary>
    public DateTime? AsanaModifiedAt { get; set; }

    public long? OwnerId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(WorkspaceId))]
    public virtual Workspace Workspace { get; set; } = null!;

    [ForeignKey(nameof(OwnerId))]
    public virtual User? Owner { get; set; }

    public virtual ICollection<AsanaTask> Tasks { get; set; } = new List<AsanaTask>();
}
