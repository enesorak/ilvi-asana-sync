using System.ComponentModel.DataAnnotations.Schema;

namespace Ilvi.Asana.Domain.Entities;

/// <summary>
/// Task yorumu veya sistem mesajı
/// </summary>
public class Story : BaseEntity
{
    public long TaskId { get; set; }

    public long? CreatedById { get; set; }

    /// <summary>
    /// Story tipi: comment, system
    /// </summary>
    [Column(TypeName = "nvarchar(50)")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Alt tip: added_to_project, comment_added, due_date_changed vb.
    /// </summary>
    [Column(TypeName = "nvarchar(100)")]
    public string? ResourceSubtype { get; set; }

    /// <summary>
    /// Yorum metni
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Text { get; set; }

    /// <summary>
    /// Asana'da oluşturulma tarihi
    /// </summary>
    public DateTime? AsanaCreatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(TaskId))]
    public virtual AsanaTask Task { get; set; } = null!;

    [ForeignKey(nameof(CreatedById))]
    public virtual User? CreatedBy { get; set; }
}
