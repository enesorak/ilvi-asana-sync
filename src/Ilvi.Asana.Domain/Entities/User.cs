using System.ComponentModel.DataAnnotations.Schema;

namespace Ilvi.Asana.Domain.Entities;

/// <summary>
/// Asana kullanıcısı
/// </summary>
public class User : BaseEntity
{
    [Column(TypeName = "nvarchar(200)")]
    public string Name { get; set; } = null!;

    [Column(TypeName = "nvarchar(200)")]
    public string? Email { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? PhotoUrl { get; set; }

    // Navigation properties
    public virtual ICollection<AsanaTask> AssignedTasks { get; set; } = new List<AsanaTask>();
    public virtual ICollection<Story> CreatedStories { get; set; } = new List<Story>();
}
