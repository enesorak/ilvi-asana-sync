using Ilvi.Asana.Domain.Entities;
using Ilvi.Asana.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ilvi.Asana.Application.Services;

/// <summary>
/// Ana senkronizasyon orkestrasyonu
/// </summary>
public class SyncOrchestrator : ISyncService
{
    private readonly IAsanaApiClient _asanaClient;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Workspace> _workspaceRepo;
    private readonly IRepository<Project> _projectRepo;
    private readonly IRepository<AsanaTask> _taskRepo;
    private readonly IRepository<TaskDependency> _dependencyRepo;
    private readonly IRepository<Attachment> _attachmentRepo;
    private readonly IRepository<Story> _storyRepo;
    private readonly IRepository<SyncLog> _syncLogRepo;
    private readonly IRepository<SyncConfiguration> _configRepo;
    private readonly IStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SyncOrchestrator> _logger;

    private static CancellationTokenSource? _currentSyncCts;
    private static readonly object _syncLock = new();
    private static SyncStats _currentProgress = new(0, 0, 0, 0, 0, 0, 0, 0);

    public SyncOrchestrator(
        IAsanaApiClient asanaClient,
        IRepository<User> userRepo,
        IRepository<Workspace> workspaceRepo,
        IRepository<Project> projectRepo,
        IRepository<AsanaTask> taskRepo,
        IRepository<TaskDependency> dependencyRepo,
        IRepository<Attachment> attachmentRepo,
        IRepository<Story> storyRepo,
        IRepository<SyncLog> syncLogRepo,
        IRepository<SyncConfiguration> configRepo,
        IStorageService storageService,
        IUnitOfWork unitOfWork,
        ILogger<SyncOrchestrator> logger)
    {
        _asanaClient = asanaClient;
        _userRepo = userRepo;
        _workspaceRepo = workspaceRepo;
        _projectRepo = projectRepo;
        _taskRepo = taskRepo;
        _dependencyRepo = dependencyRepo;
        _attachmentRepo = attachmentRepo;
        _storyRepo = storyRepo;
        _syncLogRepo = syncLogRepo;
        _configRepo = configRepo;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SyncLog> ExecuteFullSyncAsync(CancellationToken ct = default)
    {
        // Eğer zaten çalışan bir sync varsa, hata döndür
        lock (_syncLock)
        {
            if (_currentSyncCts != null && !_currentSyncCts.IsCancellationRequested)
            {
                throw new InvalidOperationException("Sync zaten çalışıyor. Lütfen bekleyin veya iptal edin.");
            }
            _currentSyncCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        }

        var linkedCt = _currentSyncCts.Token;
        _asanaClient.ResetApiCallCount();
        _currentProgress = new SyncStats(0, 0, 0, 0, 0, 0, 0, 0);

        // Sync log oluştur
        var syncLog = new SyncLog
        {
            StartedAt = DateTime.UtcNow,
            Status = "Running"
        };
        await _syncLogRepo.AddAsync(syncLog, linkedCt);
        await _unitOfWork.SaveChangesAsync(linkedCt);

        try
        {
            _logger.LogInformation("🚀 Full sync başlatılıyor...");

            // 1. Users
            _logger.LogInformation("👥 Users senkronize ediliyor...");
            var userCount = await SyncUsersAsync(linkedCt);
            syncLog.UsersCount = userCount;
            UpdateProgress(users: userCount);

            // 2. Workspaces
            _logger.LogInformation("🏢 Workspaces senkronize ediliyor...");
            var workspaces = await SyncWorkspacesAsync(linkedCt);
            syncLog.WorkspacesCount = workspaces.Count;
            UpdateProgress(workspaces: workspaces.Count);

            // 3. Projects (her workspace için)
            _logger.LogInformation("📁 Projects senkronize ediliyor...");
            var allProjects = new List<Project>();
            foreach (var workspace in workspaces)
            {
                linkedCt.ThrowIfCancellationRequested();
                var projects = await SyncProjectsAsync(workspace.Id, linkedCt);
                allProjects.AddRange(projects);
                UpdateProgress(projects: allProjects.Count);
            }
            syncLog.ProjectsCount = allProjects.Count;

            // 4. Tasks (paralel, batch halinde)
            _logger.LogInformation("📋 Tasks senkronize ediliyor ({Count} proje)...", allProjects.Count);
            var taskCount = 0;
            var storyCount = 0;
            var attachmentCount = 0;
            var downloadedCount = 0;

            // Config al
            var config = await _configRepo.FirstOrDefaultAsync(_ => true, linkedCt)
                ?? new SyncConfiguration();

            // Projeleri 5'li batch'ler halinde işle
            var projectBatches = allProjects.Chunk(5).ToList();
            var batchIndex = 0;

            foreach (var batch in projectBatches)
            {
                linkedCt.ThrowIfCancellationRequested();
                batchIndex++;
                _logger.LogInformation("  Batch {Current}/{Total} işleniyor...", batchIndex, projectBatches.Count);

                // Her batch içindeki projeleri paralel işle
                var tasks = batch.Select(async project =>
                {
                    try
                    {
                        // Tasks
                        var projectTasks = await SyncTasksForProjectAsync(project.Id, linkedCt);
                        Interlocked.Add(ref taskCount, projectTasks.Count);
                        UpdateProgress(tasks: taskCount);

                        // Her task için dependencies, stories, attachments
                        foreach (var task in projectTasks)
                        {
                            linkedCt.ThrowIfCancellationRequested();

                            // Dependencies
                            await SyncTaskDependenciesAsync(task.Id, linkedCt);

                            // Stories
                            var stories = await SyncTaskStoriesAsync(task.Id, linkedCt);
                            Interlocked.Add(ref storyCount, stories);
                            UpdateProgress(stories: storyCount);

                            // Attachments
                            var (attCount, dlCount) = await SyncTaskAttachmentsAsync(task.Id, config, linkedCt);
                            Interlocked.Add(ref attachmentCount, attCount);
                            Interlocked.Add(ref downloadedCount, dlCount);
                            UpdateProgress(attachments: attachmentCount, downloaded: downloadedCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Proje {ProjectId} sync hatası", project.Id);
                    }
                });

                await Task.WhenAll(tasks);
            }

            syncLog.TasksCount = taskCount;
            syncLog.StoriesCount = storyCount;
            syncLog.AttachmentsCount = attachmentCount;
            syncLog.DownloadedAttachmentsCount = downloadedCount;
            syncLog.ApiCallsCount = _asanaClient.GetApiCallCount();
            syncLog.Status = "Completed";
            syncLog.CompletedAt = DateTime.UtcNow;

            // Config güncelle
            config.LastSuccessfulSyncAt = DateTime.UtcNow;
            if (config.Id == 0)
                await _configRepo.AddAsync(config, linkedCt);
            else
                _configRepo.Update(config);

            await _unitOfWork.SaveChangesAsync(linkedCt);

            _logger.LogInformation(
                "✅ Sync tamamlandı! Users: {Users}, Projects: {Projects}, Tasks: {Tasks}, Stories: {Stories}, Attachments: {Attachments}, Süre: {Duration}s",
                syncLog.UsersCount, syncLog.ProjectsCount, syncLog.TasksCount, 
                syncLog.StoriesCount, syncLog.AttachmentsCount, syncLog.DurationSeconds);

            return syncLog;
        }
        catch (OperationCanceledException)
        {
            syncLog.Status = "Cancelled";
            syncLog.CompletedAt = DateTime.UtcNow;
            syncLog.ErrorMessage = "Sync iptal edildi";
            _logger.LogWarning("⚠️ Sync iptal edildi");
            throw;
        }
        catch (Exception ex)
        {
            syncLog.Status = "Failed";
            syncLog.CompletedAt = DateTime.UtcNow;
            syncLog.ErrorMessage = ex.Message;
            syncLog.ErrorStackTrace = ex.StackTrace;
            syncLog.ApiCallsCount = _asanaClient.GetApiCallCount();
            _logger.LogError(ex, "❌ Sync başarısız!");
            throw;
        }
        finally
        {
            _syncLogRepo.Update(syncLog);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            
            lock (_syncLock)
            {
                _currentSyncCts?.Dispose();
                _currentSyncCts = null;
            }
        }
    }

    private void UpdateProgress(int? users = null, int? workspaces = null, int? projects = null,
        int? tasks = null, int? stories = null, int? attachments = null, int? downloaded = null)
    {
        _currentProgress = _currentProgress with
        {
            UsersCount = users ?? _currentProgress.UsersCount,
            WorkspacesCount = workspaces ?? _currentProgress.WorkspacesCount,
            ProjectsCount = projects ?? _currentProgress.ProjectsCount,
            TasksCount = tasks ?? _currentProgress.TasksCount,
            StoriesCount = stories ?? _currentProgress.StoriesCount,
            AttachmentsCount = attachments ?? _currentProgress.AttachmentsCount,
            DownloadedAttachmentsCount = downloaded ?? _currentProgress.DownloadedAttachmentsCount,
            ApiCallsCount = _asanaClient.GetApiCallCount()
        };
    }

    #region Individual Sync Methods

    private async Task<int> SyncUsersAsync(CancellationToken ct)
    {
        var asanaUsers = await _asanaClient.GetUsersAsync(ct);
        var users = asanaUsers.Select(u => new User
        {
            Id = u.Gid,
            Name = u.Name,
            Email = u.Email,
            PhotoUrl = u.PhotoUrl,
            JsonData = u.RawJson,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await _userRepo.BulkInsertOrUpdateAsync(users, ct);
        return users.Count;
    }

    private async Task<List<Workspace>> SyncWorkspacesAsync(CancellationToken ct)
    {
        var asanaWorkspaces = await _asanaClient.GetWorkspacesAsync(ct);
        var workspaces = asanaWorkspaces.Select(w => new Workspace
        {
            Id = w.Gid,
            Name = w.Name,
            IsOrganization = w.IsOrganization,
            JsonData = w.RawJson,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await _workspaceRepo.BulkInsertOrUpdateAsync(workspaces, ct);
        return workspaces;
    }

    private async Task<List<Project>> SyncProjectsAsync(long workspaceId, CancellationToken ct)
    {
        var asanaProjects = await _asanaClient.GetProjectsAsync(workspaceId, ct);
        var projects = asanaProjects.Select(p => new Project
        {
            Id = p.Gid,
            WorkspaceId = workspaceId,
            Name = p.Name,
            Archived = p.Archived,
            Color = p.Color,
            Notes = p.Notes,
            DueDate = p.DueDate,
            AsanaCreatedAt = p.CreatedAt,
            AsanaModifiedAt = p.ModifiedAt,
            OwnerId = p.OwnerId,
            JsonData = p.RawJson,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await _projectRepo.BulkInsertOrUpdateAsync(projects, ct);
        return projects;
    }

    private async Task<List<AsanaTask>> SyncTasksForProjectAsync(long projectId, CancellationToken ct)
    {
        var asanaTasks = await _asanaClient.GetTasksForProjectAsync(projectId, ct);
        var tasks = asanaTasks.Select(t => new AsanaTask
        {
            Id = t.Gid,
            ProjectId = projectId,
            Name = t.Name,
            Notes = t.Notes,
            HtmlNotes = t.HtmlNotes,
            Completed = t.Completed,
            CompletedAt = t.CompletedAt,
            CompletedById = t.CompletedById,
            DueOn = t.DueOn,
            DueAt = t.DueAt,
            StartOn = t.StartOn,
            StartAt = t.StartAt,
            AsanaCreatedAt = t.CreatedAt,
            AsanaModifiedAt = t.ModifiedAt,
            AssigneeId = t.AssigneeId,
            CustomFieldsJson = t.CustomFieldsJson,
            MembershipsJson = t.MembershipsJson,
            NumSubtasks = t.NumSubtasks,
            ParentTaskId = t.ParentTaskId,
            ResourceSubtype = t.ResourceSubtype,
            JsonData = t.RawJson,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await _taskRepo.BulkInsertOrUpdateAsync(tasks, ct);
        return tasks;
    }

    private async Task SyncTaskDependenciesAsync(long taskId, CancellationToken ct)
    {
        var asanaDeps = await _asanaClient.GetTaskDependenciesAsync(taskId, ct);
        
        // Önce bu task'ın mevcut dependency'lerini sil
        var existingDeps = await _dependencyRepo.FindAsync(d => d.TaskId == taskId, ct);
        if (existingDeps.Any())
        {
            _dependencyRepo.RemoveRange(existingDeps);
        }

        // Yeni dependency'leri ekle
        var deps = asanaDeps.Select(d => new TaskDependency
        {
            TaskId = taskId,
            DependsOnTaskId = d.Gid,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        if (deps.Any())
        {
            await _dependencyRepo.AddRangeAsync(deps, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<int> SyncTaskStoriesAsync(long taskId, CancellationToken ct)
    {
        var asanaStories = await _asanaClient.GetTaskStoriesAsync(taskId, ct);
        var stories = asanaStories.Select(s => new Story
        {
            Id = s.Gid,
            TaskId = taskId,
            Type = s.Type,
            ResourceSubtype = s.ResourceSubtype,
            Text = s.Text,
            CreatedById = s.CreatedById,
            AsanaCreatedAt = s.CreatedAt,
            JsonData = s.RawJson,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await _storyRepo.BulkInsertOrUpdateAsync(stories, ct);
        return stories.Count;
    }

    private async Task<(int attachmentCount, int downloadedCount)> SyncTaskAttachmentsAsync(
        long taskId, SyncConfiguration config, CancellationToken ct)
    {
        var asanaAttachments = await _asanaClient.GetTaskAttachmentsAsync(taskId, ct);
        var downloadedCount = 0;

        var attachments = asanaAttachments.Select(a => new Attachment
        {
            Id = a.Gid,
            TaskId = taskId,
            Name = a.Name,
            DownloadUrl = a.DownloadUrl,
            ViewUrl = a.ViewUrl,
            PermanentUrl = a.PermanentUrl,
            Host = a.Host,
            AsanaCreatedAt = a.CreatedAt,
            JsonData = a.RawJson,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        // Mevcut attachment'ları al (download durumunu korumak için)
        var existingAttachments = await _attachmentRepo.FindAsync(
            a => attachments.Select(x => x.Id).Contains(a.Id), ct);
        var existingDict = existingAttachments.ToDictionary(a => a.Id);

        foreach (var attachment in attachments)
        {
            // Eğer daha önce indirilmişse, path'leri koru
            if (existingDict.TryGetValue(attachment.Id, out var existing) && existing.IsDownloaded)
            {
                attachment.LocalPath = existing.LocalPath;
                attachment.ThumbnailPath = existing.ThumbnailPath;
                attachment.FileSize = existing.FileSize;
                attachment.IsDownloaded = true;
                continue;
            }

            // Download aktifse ve URL varsa indir
            if (config.DownloadAttachments && !string.IsNullOrEmpty(attachment.DownloadUrl))
            {
                try
                {
                    var result = await _storageService.DownloadAndSaveAsync(
                        attachment.DownloadUrl!,
                        $"{attachment.Id}_{attachment.Name}",
                        ct);

                    if (result.Success)
                    {
                        attachment.LocalPath = result.OriginalPath;
                        attachment.ThumbnailPath = result.ThumbnailPath;
                        attachment.FileSize = result.FileSize;
                        attachment.IsDownloaded = true;
                        downloadedCount++;
                    }
                    else
                    {
                        attachment.DownloadError = result.ErrorMessage;
                    }
                }
                catch (Exception ex)
                {
                    attachment.DownloadError = ex.Message;
                    _logger.LogWarning(ex, "Attachment {Id} indirilemedi", attachment.Id);
                }
            }
        }

        await _attachmentRepo.BulkInsertOrUpdateAsync(attachments, ct);
        return (attachments.Count, downloadedCount);
    }

    #endregion

    #region Status Methods

    public async Task<SyncStatusInfo> GetCurrentStatusAsync(CancellationToken ct = default)
    {
        var isRunning = false;
        lock (_syncLock)
        {
            isRunning = _currentSyncCts != null && !_currentSyncCts.IsCancellationRequested;
        }

        var lastLog = await _syncLogRepo.Query()
            .OrderByDescending(l => l.StartedAt)
            .FirstOrDefaultAsync(ct);

        return new SyncStatusInfo(
            IsRunning: isRunning,
            LastSyncStartedAt: lastLog?.StartedAt,
            LastSyncCompletedAt: lastLog?.CompletedAt,
            LastSyncStatus: lastLog?.Status,
            LastSyncDurationSeconds: lastLog?.DurationSeconds,
            CurrentProgress: isRunning ? _currentProgress : null
        );
    }

    public Task CancelCurrentSyncAsync()
    {
        lock (_syncLock)
        {
            if (_currentSyncCts != null && !_currentSyncCts.IsCancellationRequested)
            {
                _currentSyncCts.Cancel();
                _logger.LogInformation("🛑 Sync iptal isteği gönderildi");
            }
        }
        return Task.CompletedTask;
    }

    #endregion
}

// Extension for FirstOrDefaultAsync
public static class QueryableExtensions
{
    public static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> query, CancellationToken ct = default)
    {
        // Bu normalde EF Core tarafından sağlanır, burada basit bir implementasyon
        return await Task.Run(() => query.FirstOrDefault(), ct);
    }
}
