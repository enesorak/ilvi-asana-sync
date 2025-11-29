using System.Net;
using System.Text.Json;
using Ilvi.Asana.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ilvi.Asana.Infrastructure.AsanaApi;

/// <summary>
/// Asana REST API client
/// opt_fields kullanarak minimum API çağrısı ile maksimum veri çeker
/// </summary>
public class AsanaApiClient : IAsanaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AsanaRateLimiter _rateLimiter;
    private readonly ILogger<AsanaApiClient> _logger;
    private int _apiCallCount;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public AsanaApiClient(HttpClient httpClient, AsanaRateLimiter rateLimiter, ILogger<AsanaApiClient> logger)
    {
        _httpClient = httpClient;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    #region Public Methods

    public async Task<List<AsanaUserDto>> GetUsersAsync(CancellationToken ct = default)
    {
        const string optFields = "name,email,photo.image_128x128";
        var response = await GetAllPagesAsync($"users?opt_fields={optFields}", ct);
        
        return response.Select(json =>
        {
            var gid = json.GetProperty("gid").GetString()!;
            var name = json.GetProperty("name").GetString() ?? "";
            var email = json.TryGetProperty("email", out var e) ? e.GetString() : null;
            var photoUrl = json.TryGetProperty("photo", out var p) && p.ValueKind != JsonValueKind.Null
                ? p.TryGetProperty("image_128x128", out var img) ? img.GetString() : null
                : null;

            return new AsanaUserDto(
                Gid: long.Parse(gid),
                Name: name,
                Email: email,
                PhotoUrl: photoUrl,
                RawJson: json.GetRawText()
            );
        }).ToList();
    }

    public async Task<List<AsanaWorkspaceDto>> GetWorkspacesAsync(CancellationToken ct = default)
    {
        const string optFields = "name,is_organization";
        var response = await GetAllPagesAsync($"workspaces?opt_fields={optFields}", ct);

        return response.Select(json =>
        {
            var gid = json.GetProperty("gid").GetString()!;
            var name = json.GetProperty("name").GetString() ?? "";
            var isOrg = json.TryGetProperty("is_organization", out var o) && o.GetBoolean();

            return new AsanaWorkspaceDto(
                Gid: long.Parse(gid),
                Name: name,
                IsOrganization: isOrg,
                RawJson: json.GetRawText()
            );
        }).ToList();
    }

    public async Task<List<AsanaProjectDto>> GetProjectsAsync(long workspaceId, CancellationToken ct = default)
    {
        const string optFields = "name,archived,color,notes,due_date,due_on,created_at,modified_at,owner,owner.name";
        var response = await GetAllPagesAsync($"workspaces/{workspaceId}/projects?opt_fields={optFields}", ct);

        return response.Select(json =>
        {
            var gid = json.GetProperty("gid").GetString()!;
            
            return new AsanaProjectDto(
                Gid: long.Parse(gid),
                WorkspaceId: workspaceId,
                Name: json.GetProperty("name").GetString() ?? "",
                Archived: json.TryGetProperty("archived", out var a) && a.GetBoolean(),
                Color: json.TryGetProperty("color", out var c) ? c.GetString() : null,
                Notes: json.TryGetProperty("notes", out var n) ? n.GetString() : null,
                DueDate: ParseNullableDateTime(json, "due_date") ?? ParseNullableDateTime(json, "due_on"),
                CreatedAt: ParseNullableDateTime(json, "created_at"),
                ModifiedAt: ParseNullableDateTime(json, "modified_at"),
                OwnerId: ParseNestedId(json, "owner"),
                RawJson: json.GetRawText()
            );
        }).ToList();
    }

    public async Task<List<AsanaTaskDto>> GetTasksForProjectAsync(long projectId, CancellationToken ct = default)
    {
        // opt_fields ile tüm detayları tek seferde çekiyoruz - ayrı TaskDetails API çağrısına gerek yok!
        var optFields = string.Join(",", new[]
        {
            "name", "notes", "html_notes",
            "completed", "completed_at", "completed_by", "completed_by.name",
            "created_at", "modified_at",
            "due_on", "due_at", "start_on", "start_at",
            "assignee", "assignee.name", "assignee.email",
            "custom_fields", "custom_fields.name", "custom_fields.display_value", "custom_fields.type",
            "memberships", "memberships.section", "memberships.section.name",
            "num_subtasks", "parent", "resource_subtype"
        });

        var response = await GetAllPagesAsync($"projects/{projectId}/tasks?opt_fields={optFields}", ct);

        return response.Select(json =>
        {
            var gid = json.GetProperty("gid").GetString()!;

            // Custom fields JSON olarak
            string? customFieldsJson = null;
            if (json.TryGetProperty("custom_fields", out var cf) && cf.ValueKind == JsonValueKind.Array)
            {
                customFieldsJson = cf.GetRawText();
            }

            // Memberships JSON olarak
            string? membershipsJson = null;
            if (json.TryGetProperty("memberships", out var m) && m.ValueKind == JsonValueKind.Array)
            {
                membershipsJson = m.GetRawText();
            }

            return new AsanaTaskDto(
                Gid: long.Parse(gid),
                Name: json.GetProperty("name").GetString() ?? "",
                Notes: json.TryGetProperty("notes", out var n) ? n.GetString() : null,
                HtmlNotes: json.TryGetProperty("html_notes", out var hn) ? hn.GetString() : null,
                Completed: json.TryGetProperty("completed", out var comp) && comp.GetBoolean(),
                CompletedAt: ParseNullableDateTime(json, "completed_at"),
                CompletedById: ParseNestedId(json, "completed_by"),
                DueOn: ParseNullableDateTime(json, "due_on"),
                DueAt: ParseNullableDateTime(json, "due_at"),
                StartOn: ParseNullableDateTime(json, "start_on"),
                StartAt: ParseNullableDateTime(json, "start_at"),
                CreatedAt: ParseNullableDateTime(json, "created_at"),
                ModifiedAt: ParseNullableDateTime(json, "modified_at"),
                AssigneeId: ParseNestedId(json, "assignee"),
                AssigneeName: ParseNestedString(json, "assignee", "name"),
                CustomFieldsJson: customFieldsJson,
                MembershipsJson: membershipsJson,
                NumSubtasks: json.TryGetProperty("num_subtasks", out var ns) ? ns.GetInt32() : 0,
                ParentTaskId: ParseNestedId(json, "parent"),
                ResourceSubtype: json.TryGetProperty("resource_subtype", out var rs) ? rs.GetString() : null,
                RawJson: json.GetRawText()
            );
        }).ToList();
    }

    public async Task<List<AsanaTaskDependencyDto>> GetTaskDependenciesAsync(long taskId, CancellationToken ct = default)
    {
        const string optFields = "gid,name";
        var response = await GetAllPagesAsync($"tasks/{taskId}/dependencies?opt_fields={optFields}", ct);

        return response.Select(json => new AsanaTaskDependencyDto(
            Gid: long.Parse(json.GetProperty("gid").GetString()!),
            Name: json.GetProperty("name").GetString() ?? ""
        )).ToList();
    }

    public async Task<List<AsanaAttachmentDto>> GetTaskAttachmentsAsync(long taskId, CancellationToken ct = default)
    {
        const string optFields = "gid,name,download_url,view_url,permanent_url,host,created_at";
        var response = await GetAllPagesAsync($"tasks/{taskId}/attachments?opt_fields={optFields}", ct);

        return response.Select(json => new AsanaAttachmentDto(
            Gid: long.Parse(json.GetProperty("gid").GetString()!),
            Name: json.GetProperty("name").GetString() ?? "",
            DownloadUrl: json.TryGetProperty("download_url", out var d) ? d.GetString() : null,
            ViewUrl: json.TryGetProperty("view_url", out var v) ? v.GetString() : null,
            PermanentUrl: json.TryGetProperty("permanent_url", out var p) ? p.GetString() : null,
            Host: json.TryGetProperty("host", out var h) ? h.GetString() : null,
            CreatedAt: ParseNullableDateTime(json, "created_at"),
            RawJson: json.GetRawText()
        )).ToList();
    }

    public async Task<List<AsanaStoryDto>> GetTaskStoriesAsync(long taskId, CancellationToken ct = default)
    {
        const string optFields = "gid,type,resource_subtype,text,created_at,created_by,created_by.name";
        var response = await GetAllPagesAsync($"tasks/{taskId}/stories?opt_fields={optFields}", ct);

        return response.Select(json => new AsanaStoryDto(
            Gid: long.Parse(json.GetProperty("gid").GetString()!),
            Type: json.TryGetProperty("type", out var t) ? t.GetString() ?? "system" : "system",
            ResourceSubtype: json.TryGetProperty("resource_subtype", out var rs) ? rs.GetString() : null,
            Text: json.TryGetProperty("text", out var txt) ? txt.GetString() : null,
            CreatedById: ParseNestedId(json, "created_by"),
            CreatedByName: ParseNestedString(json, "created_by", "name"),
            CreatedAt: ParseNullableDateTime(json, "created_at"),
            RawJson: json.GetRawText()
        )).ToList();
    }

    public int GetApiCallCount() => _apiCallCount;
    public void ResetApiCallCount() => _apiCallCount = 0;

    #endregion

    #region Private Methods

    private async Task<List<JsonElement>> GetAllPagesAsync(string endpoint, CancellationToken ct)
    {
        var results = new List<JsonElement>();
        string? offset = null;
        var separator = endpoint.Contains('?') ? "&" : "?";

        do
        {
            var url = offset == null
                ? $"{endpoint}{separator}limit=100"
                : $"{endpoint}{separator}limit=100&offset={offset}";

            var response = await _rateLimiter.ExecuteAsync(async () =>
            {
                Interlocked.Increment(ref _apiCallCount);
                var resp = await _httpClient.GetAsync(url, ct);

                // Rate limit kontrolü
                if (resp.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = resp.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
                    _logger.LogWarning("⚠️ Rate limited! {Seconds}s bekleniyor...", retryAfter.TotalSeconds);
                    await Task.Delay(retryAfter, ct);
                    
                    // Tekrar dene
                    Interlocked.Increment(ref _apiCallCount);
                    resp = await _httpClient.GetAsync(url, ct);
                }

                resp.EnsureSuccessStatusCode();
                return resp;
            }, ct);

            var content = await response.Content.ReadAsStringAsync(ct);
            
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Data array'ini al
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in data.EnumerateArray())
                {
                    results.Add(item.Clone());
                }
            }

            // Next page kontrolü
            offset = null;
            if (root.TryGetProperty("next_page", out var nextPage) && nextPage.ValueKind != JsonValueKind.Null)
            {
                if (nextPage.TryGetProperty("offset", out var offsetProp))
                {
                    offset = offsetProp.GetString();
                }
            }

        } while (offset != null);

        return results;
    }

    private static DateTime? ParseNullableDateTime(JsonElement json, string propertyName)
    {
        if (!json.TryGetProperty(propertyName, out var prop) || prop.ValueKind == JsonValueKind.Null)
            return null;

        var str = prop.GetString();
        if (string.IsNullOrEmpty(str))
            return null;

        // Asana tarih formatları: "2024-01-15" veya "2024-01-15T10:30:00.000Z"
        if (DateTime.TryParse(str, out var dt))
            return dt;

        return null;
    }

    private static long? ParseNestedId(JsonElement json, string propertyName)
    {
        if (!json.TryGetProperty(propertyName, out var prop) || prop.ValueKind == JsonValueKind.Null)
            return null;

        if (prop.TryGetProperty("gid", out var gid))
        {
            var gidStr = gid.GetString();
            if (long.TryParse(gidStr, out var id))
                return id;
        }

        return null;
    }

    private static string? ParseNestedString(JsonElement json, string propertyName, string nestedPropertyName)
    {
        if (!json.TryGetProperty(propertyName, out var prop) || prop.ValueKind == JsonValueKind.Null)
            return null;

        if (prop.TryGetProperty(nestedPropertyName, out var nested))
            return nested.GetString();

        return null;
    }

    #endregion
}
