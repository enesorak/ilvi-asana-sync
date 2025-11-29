using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ilvi.Asana.Domain.Entities;

/// <summary>
/// Senkronizasyon log kaydı
/// </summary>
public class SyncLog
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Başlangıç zamanı
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Bitiş zamanı
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Durum: Running, Completed, Failed, Cancelled
    /// </summary>
    [Column(TypeName = "nvarchar(50)")]
    public string Status { get; set; } = "Running";

    /// <summary>
    /// Senkronize edilen user sayısı
    /// </summary>
    public int UsersCount { get; set; }

    /// <summary>
    /// Senkronize edilen workspace sayısı
    /// </summary>
    public int WorkspacesCount { get; set; }

    /// <summary>
    /// Senkronize edilen proje sayısı
    /// </summary>
    public int ProjectsCount { get; set; }

    /// <summary>
    /// Senkronize edilen task sayısı
    /// </summary>
    public int TasksCount { get; set; }

    /// <summary>
    /// Senkronize edilen story sayısı
    /// </summary>
    public int StoriesCount { get; set; }

    /// <summary>
    /// Senkronize edilen attachment sayısı
    /// </summary>
    public int AttachmentsCount { get; set; }

    /// <summary>
    /// İndirilen attachment sayısı
    /// </summary>
    public int DownloadedAttachmentsCount { get; set; }

    /// <summary>
    /// Toplam API çağrısı sayısı
    /// </summary>
    public int ApiCallsCount { get; set; }

    /// <summary>
    /// Hata mesajı (varsa)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Hata stack trace (varsa)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// Sync süresi (saniye)
    /// </summary>
    public int? DurationSeconds => CompletedAt.HasValue 
        ? (int)(CompletedAt.Value - StartedAt).TotalSeconds 
        : null;

    /// <summary>
    /// Hangfire Job ID
    /// </summary>
    [Column(TypeName = "nvarchar(100)")]
    public string? HangfireJobId { get; set; }
}
