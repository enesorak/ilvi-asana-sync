using Hangfire;
using Ilvi.Asana.Application.DTOs;
using Ilvi.Asana.Domain.Entities;
using Ilvi.Asana.Domain.Interfaces;
using Ilvi.Asana.Web.Jobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ilvi.Asana.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly IRepository<SyncLog> _syncLogRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Workspace> _workspaceRepo;
    private readonly IRepository<Project> _projectRepo;
    private readonly IRepository<AsanaTask> _taskRepo;
    private readonly IRepository<Story> _storyRepo;
    private readonly IRepository<Attachment> _attachmentRepo;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        ISyncService syncService,
        IRepository<SyncLog> syncLogRepo,
        IRepository<User> userRepo,
        IRepository<Workspace> workspaceRepo,
        IRepository<Project> projectRepo,
        IRepository<AsanaTask> taskRepo,
        IRepository<Story> storyRepo,
        IRepository<Attachment> attachmentRepo,
        ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _syncLogRepo = syncLogRepo;
        _userRepo = userRepo;
        _workspaceRepo = workspaceRepo;
        _projectRepo = projectRepo;
        _taskRepo = taskRepo;
        _storyRepo = storyRepo;
        _attachmentRepo = attachmentRepo;
        _logger = logger;
    }

    /// <summary>
    /// Manuel sync başlatır
    /// </summary>
    [HttpPost("start")]
    public IActionResult StartSync()
    {
        _logger.LogInformation("📤 Manuel sync isteği alındı");
        
        // Background job olarak başlat
        var jobId = BackgroundJob.Enqueue<SyncJob>(job => job.ExecuteAsync(CancellationToken.None));
        
        return Ok(new { jobId, message = "Sync başlatıldı" });
    }

    /// <summary>
    /// Çalışan sync'i iptal eder
    /// </summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelSync()
    {
        _logger.LogInformation("🛑 Sync iptal isteği alındı");
        await _syncService.CancelCurrentSyncAsync();
        return Ok(new { message = "Sync iptal edildi" });
    }

    /// <summary>
    /// Mevcut sync durumunu döndürür
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<SyncStatusDto>> GetStatus()
    {
        var status = await _syncService.GetCurrentStatusAsync();
        
        return Ok(new SyncStatusDto(
            IsRunning: status.IsRunning,
            LastSyncAt: status.LastSyncCompletedAt ?? status.LastSyncStartedAt,
            LastSyncStatus: status.LastSyncStatus,
            LastSyncDurationSeconds: status.LastSyncDurationSeconds,
            CurrentProgress: status.CurrentProgress != null 
                ? new SyncProgressDto(
                    status.CurrentProgress.UsersCount,
                    status.CurrentProgress.WorkspacesCount,
                    status.CurrentProgress.ProjectsCount,
                    status.CurrentProgress.TasksCount,
                    status.CurrentProgress.StoriesCount,
                    status.CurrentProgress.AttachmentsCount,
                    status.CurrentProgress.DownloadedAttachmentsCount,
                    status.CurrentProgress.ApiCallsCount)
                : null
        ));
    }

    /// <summary>
    /// Veritabanı istatistiklerini döndürür
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<SyncStatsDto>> GetStats()
    {
        var stats = new SyncStatsDto(
            UsersCount: await _userRepo.CountAsync(),
            WorkspacesCount: await _workspaceRepo.CountAsync(),
            ProjectsCount: await _projectRepo.CountAsync(),
            TasksCount: await _taskRepo.CountAsync(),
            StoriesCount: await _storyRepo.CountAsync(),
            AttachmentsCount: await _attachmentRepo.CountAsync()
        );

        return Ok(stats);
    }

    /// <summary>
    /// Son sync loglarını döndürür
    /// </summary>
    [HttpGet("logs")]
    public async Task<ActionResult<List<SyncLogDto>>> GetLogs([FromQuery] int limit = 20)
    {
        var logs = await _syncLogRepo.Query()
            .OrderByDescending(l => l.StartedAt)
            .Take(limit)
            .Select(l => new SyncLogDto(
                l.Id,
                l.StartedAt,
                l.CompletedAt,
                l.Status,
                l.UsersCount,
                l.ProjectsCount,
                l.TasksCount,
                l.StoriesCount,
                l.AttachmentsCount,
                l.DurationSeconds,
                l.ErrorMessage
            ))
            .ToListAsync();

        return Ok(logs);
    }

    /// <summary>
    /// Belirli bir sync log detayını döndürür
    /// </summary>
    [HttpGet("logs/{id}")]
    public async Task<ActionResult<SyncLog>> GetLog(int id)
    {
        var log = await _syncLogRepo.GetByIdAsync(id);
        if (log == null)
            return NotFound();

        return Ok(log);
    }
}
