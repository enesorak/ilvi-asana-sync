namespace Ilvi.Asana.Application.DTOs;

/// <summary>
/// Sync istatistikleri
/// </summary>
public record SyncStatsDto(
    int UsersCount,
    int WorkspacesCount,
    int ProjectsCount,
    int TasksCount,
    int StoriesCount,
    int AttachmentsCount
);

/// <summary>
/// Sync durumu
/// </summary>
public record SyncStatusDto(
    bool IsRunning,
    DateTime? LastSyncAt,
    string? LastSyncStatus,
    int? LastSyncDurationSeconds,
    SyncProgressDto? CurrentProgress
);

/// <summary>
/// Mevcut sync ilerlemesi
/// </summary>
public record SyncProgressDto(
    int UsersCount,
    int WorkspacesCount,
    int ProjectsCount,
    int TasksCount,
    int StoriesCount,
    int AttachmentsCount,
    int DownloadedAttachmentsCount,
    int ApiCallsCount
);

/// <summary>
/// Sync log kaydı
/// </summary>
public record SyncLogDto(
    int Id,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string Status,
    int UsersCount,
    int ProjectsCount,
    int TasksCount,
    int StoriesCount,
    int AttachmentsCount,
    int? DurationSeconds,
    string? ErrorMessage
);

/// <summary>
/// Configuration
/// </summary>
public record ConfigurationDto(
    string CronExpression,
    bool IsEnabled,
    bool DownloadAttachments,
    bool GenerateThumbnails,
    int ThumbnailMaxWidth,
    string AttachmentBasePath,
    DateTime? LastSuccessfulSyncAt
);

/// <summary>
/// Configuration update request
/// </summary>
public record UpdateConfigurationRequest(
    string? CronExpression,
    bool? IsEnabled,
    bool? DownloadAttachments,
    bool? GenerateThumbnails,
    int? ThumbnailMaxWidth,
    string? AttachmentBasePath
);
