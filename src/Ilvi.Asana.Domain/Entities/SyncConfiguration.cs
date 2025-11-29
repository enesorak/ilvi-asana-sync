using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ilvi.Asana.Domain.Entities;

/// <summary>
/// Senkronizasyon ayarları
/// </summary>
public class SyncConfiguration
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Cron expression (örn: "0 */3 * * *" = her 3 saatte bir)
    /// </summary>
    [Column(TypeName = "nvarchar(100)")]
    public string CronExpression { get; set; } = "0 */3 * * *";

    /// <summary>
    /// Otomatik senkronizasyon aktif mi?
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Son başarılı senkronizasyon tarihi
    /// </summary>
    public DateTime? LastSuccessfulSyncAt { get; set; }

    /// <summary>
    /// Attachment indirme aktif mi?
    /// </summary>
    public bool DownloadAttachments { get; set; } = true;

    /// <summary>
    /// Thumbnail oluşturma aktif mi?
    /// </summary>
    public bool GenerateThumbnails { get; set; } = true;

    /// <summary>
    /// Thumbnail max genişlik (px)
    /// </summary>
    public int ThumbnailMaxWidth { get; set; } = 400;

    /// <summary>
    /// Attachment dosya yolu
    /// </summary>
    [Column(TypeName = "nvarchar(500)")]
    public string AttachmentBasePath { get; set; } = "./attachments";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
