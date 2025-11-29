using System.ComponentModel.DataAnnotations.Schema;

namespace Ilvi.Asana.Domain.Entities;

/// <summary>
/// Task eki (dosya, resim vb.)
/// </summary>
public class Attachment : BaseEntity
{
    public long TaskId { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Asana'dan indirme URL'i (geçici)
    /// </summary>
    [Column(TypeName = "nvarchar(2000)")]
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Kalıcı görüntüleme URL'i
    /// </summary>
    [Column(TypeName = "nvarchar(2000)")]
    public string? ViewUrl { get; set; }

    /// <summary>
    /// Kalıcı URL
    /// </summary>
    [Column(TypeName = "nvarchar(2000)")]
    public string? PermanentUrl { get; set; }

    /// <summary>
    /// Dosyanın host edildiği yer (asana, dropbox, gdrive vb.)
    /// </summary>
    [Column(TypeName = "nvarchar(50)")]
    public string? Host { get; set; }

    /// <summary>
    /// Yerel dosya yolu (orijinal)
    /// </summary>
    [Column(TypeName = "nvarchar(500)")]
    public string? LocalPath { get; set; }

    /// <summary>
    /// Yerel thumbnail yolu
    /// </summary>
    [Column(TypeName = "nvarchar(500)")]
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// İndirildi mi?
    /// </summary>
    public bool IsDownloaded { get; set; }

    /// <summary>
    /// İndirme hatası varsa mesajı
    /// </summary>
    [Column(TypeName = "nvarchar(1000)")]
    public string? DownloadError { get; set; }

    /// <summary>
    /// Dosya boyutu (bytes)
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Asana'da oluşturulma tarihi
    /// </summary>
    public DateTime? AsanaCreatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(TaskId))]
    public virtual AsanaTask Task { get; set; } = null!;
}
