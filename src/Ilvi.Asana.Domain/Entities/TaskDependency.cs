using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ilvi.Asana.Domain.Entities;

/// <summary>
/// Task bağımlılıkları (many-to-many relationship)
/// </summary>
public class TaskDependency
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Bu task
    /// </summary>
    public long TaskId { get; set; }

    /// <summary>
    /// Bağımlı olduğu task
    /// </summary>
    public long DependsOnTaskId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(TaskId))]
    public virtual AsanaTask Task { get; set; } = null!;

    [ForeignKey(nameof(DependsOnTaskId))]
    public virtual AsanaTask DependsOnTask { get; set; } = null!;
}
