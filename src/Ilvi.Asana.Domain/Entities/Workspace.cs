using System.ComponentModel.DataAnnotations.Schema;

namespace Ilvi.Asana.Domain.Entities;

/// <summary>
/// Asana workspace
/// </summary>
public class Workspace : BaseEntity
{
    [Column(TypeName = "nvarchar(200)")]
    public string Name { get; set; } = null!;

    public bool IsOrganization { get; set; }

    // Navigation properties
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
