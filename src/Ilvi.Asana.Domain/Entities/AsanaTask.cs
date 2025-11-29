using System.ComponentModel.DataAnnotations.Schema;

namespace Ilvi.Asana.Domain.Entities;

/// <summary>
/// Asana task'ı (Task + TaskDetails birleştirildi)
/// "Task" C# keyword olduğu için "AsanaTask" kullanıyoruz
/// </summary>
public class AsanaTask : BaseEntity
{
    public long ProjectId { get; set; }

    public long? AssigneeId { get; set; }

    [Column(TypeName = "nvarchar(1000)")]
    public string Name { get; set; } = null!;

    [Column(TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? HtmlNotes { get; set; }

    public bool Completed { get; set; }

    public DateTime? CompletedAt { get; set; }

    public long? CompletedById { get; set; }

    public DateTime? DueOn { get; set; }

    public DateTime? DueAt { get; set; }

    public DateTime? StartOn { get; set; }

    public DateTime? StartAt { get; set; }

    /// <summary>
    /// Asana'daki oluşturulma tarihi
    /// </summary>
    public DateTime? AsanaCreatedAt { get; set; }

    /// <summary>
    /// Asana'daki son güncelleme tarihi
    /// </summary>
    public DateTime? AsanaModifiedAt { get; set; }

    /// <summary>
    /// Custom fields JSON olarak saklanıyor
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? CustomFieldsJson { get; set; }

    /// <summary>
    /// Memberships (section bilgisi vb.) JSON olarak saklanıyor
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? MembershipsJson { get; set; }

    /// <summary>
    /// Alt görev sayısı
    /// </summary>
    public int NumSubtasks { get; set; }

    /// <summary>
    /// Parent task ID (eğer bu bir subtask ise)
    /// </summary>
    public long? ParentTaskId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? ResourceSubtype { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; } = null!;

    [ForeignKey(nameof(AssigneeId))]
    public virtual User? Assignee { get; set; }

    [ForeignKey(nameof(CompletedById))]
    public virtual User? CompletedBy { get; set; }

    [ForeignKey(nameof(ParentTaskId))]
    public virtual AsanaTask? ParentTask { get; set; }

    public virtual ICollection<TaskDependency> Dependencies { get; set; } = new List<TaskDependency>();
    public virtual ICollection<TaskDependency> Dependents { get; set; } = new List<TaskDependency>();
    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public virtual ICollection<Story> Stories { get; set; } = new List<Story>();
}
