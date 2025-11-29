namespace Ilvi.Asana.Domain.Interfaces;

/// <summary>
/// Asana API client interface
/// </summary>
public interface IAsanaApiClient
{
    /// <summary>
    /// Tüm kullanıcıları getirir
    /// </summary>
    Task<List<AsanaUserDto>> GetUsersAsync(CancellationToken ct = default);

    /// <summary>
    /// Tüm workspace'leri getirir
    /// </summary>
    Task<List<AsanaWorkspaceDto>> GetWorkspacesAsync(CancellationToken ct = default);

    /// <summary>
    /// Bir workspace'in tüm projelerini getirir
    /// </summary>
    Task<List<AsanaProjectDto>> GetProjectsAsync(long workspaceId, CancellationToken ct = default);

    /// <summary>
    /// Bir projenin tüm task'larını detaylı olarak getirir
    /// </summary>
    Task<List<AsanaTaskDto>> GetTasksForProjectAsync(long projectId, CancellationToken ct = default);

    /// <summary>
    /// Bir task'ın bağımlılıklarını getirir
    /// </summary>
    Task<List<AsanaTaskDependencyDto>> GetTaskDependenciesAsync(long taskId, CancellationToken ct = default);

    /// <summary>
    /// Bir task'ın eklerini getirir
    /// </summary>
    Task<List<AsanaAttachmentDto>> GetTaskAttachmentsAsync(long taskId, CancellationToken ct = default);

    /// <summary>
    /// Bir task'ın story'lerini (yorum, sistem mesajları) getirir
    /// </summary>
    Task<List<AsanaStoryDto>> GetTaskStoriesAsync(long taskId, CancellationToken ct = default);

    /// <summary>
    /// API çağrı sayısını döndürür (mevcut session için)
    /// </summary>
    int GetApiCallCount();

    /// <summary>
    /// API çağrı sayısını sıfırlar
    /// </summary>
    void ResetApiCallCount();
}

#region DTOs

public record AsanaUserDto(
    long Gid,
    string Name,
    string? Email,
    string? PhotoUrl,
    string RawJson
);

public record AsanaWorkspaceDto(
    long Gid,
    string Name,
    bool IsOrganization,
    string RawJson
);

public record AsanaProjectDto(
    long Gid,
    long WorkspaceId,
    string Name,
    bool Archived,
    string? Color,
    string? Notes,
    DateTime? DueDate,
    DateTime? CreatedAt,
    DateTime? ModifiedAt,
    long? OwnerId,
    string RawJson
);

public record AsanaTaskDto(
    long Gid,
    string Name,
    string? Notes,
    string? HtmlNotes,
    bool Completed,
    DateTime? CompletedAt,
    long? CompletedById,
    DateTime? DueOn,
    DateTime? DueAt,
    DateTime? StartOn,
    DateTime? StartAt,
    DateTime? CreatedAt,
    DateTime? ModifiedAt,
    long? AssigneeId,
    string? AssigneeName,
    string? CustomFieldsJson,
    string? MembershipsJson,
    int NumSubtasks,
    long? ParentTaskId,
    string? ResourceSubtype,
    string RawJson
);

public record AsanaTaskDependencyDto(
    long Gid,
    string Name
);

public record AsanaAttachmentDto(
    long Gid,
    string Name,
    string? DownloadUrl,
    string? ViewUrl,
    string? PermanentUrl,
    string? Host,
    DateTime? CreatedAt,
    string RawJson
);

public record AsanaStoryDto(
    long Gid,
    string Type,
    string? ResourceSubtype,
    string? Text,
    long? CreatedById,
    string? CreatedByName,
    DateTime? CreatedAt,
    string RawJson
);

#endregion
