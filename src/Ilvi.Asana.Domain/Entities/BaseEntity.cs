using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ilvi.Asana.Domain.Entities;

/// <summary>
/// Tüm Asana entity'leri için temel sınıf
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Asana GID (Global ID) - Primary Key
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; }

    /// <summary>
    /// Asana'dan gelen ham JSON verisi
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string JsonData { get; set; } = "{}";

    /// <summary>
    /// Kaydın oluşturulma tarihi (local DB)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Kaydın güncellenme tarihi (local DB)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
