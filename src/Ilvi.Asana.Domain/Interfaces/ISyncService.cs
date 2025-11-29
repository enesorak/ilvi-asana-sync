using Ilvi.Asana.Domain.Entities;

namespace Ilvi.Asana.Domain.Interfaces;

/// <summary>
/// Senkronizasyon servisi interface
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Tam senkronizasyon çalıştırır
    /// </summary>
    Task<SyncLog> ExecuteFullSyncAsync(CancellationToken ct = default);

    /// <summary>
    /// Mevcut sync durumunu döndürür
    /// </summary>
    Task<SyncStatusInfo> GetCurrentStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Çalışan sync'i iptal eder
    /// </summary>
    Task CancelCurrentSyncAsync();
}

public record SyncStatusInfo(
    bool IsRunning,
    DateTime? LastSyncStartedAt,
    DateTime? LastSyncCompletedAt,
    string? LastSyncStatus,
    int? LastSyncDurationSeconds,
    SyncStats? CurrentProgress
);

public record SyncStats(
    int UsersCount,
    int WorkspacesCount,
    int ProjectsCount,
    int TasksCount,
    int StoriesCount,
    int AttachmentsCount,
    int DownloadedAttachmentsCount,
    int ApiCallsCount
);
