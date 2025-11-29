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
public class ConfigurationController : ControllerBase
{
    private readonly IRepository<SyncConfiguration> _configRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(
        IRepository<SyncConfiguration> configRepo,
        IUnitOfWork unitOfWork,
        ILogger<ConfigurationController> logger)
    {
        _configRepo = configRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Mevcut ayarları döndürür
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ConfigurationDto>> GetConfiguration()
    {
        var config = await _configRepo.Query().FirstOrDefaultAsync();
        
        if (config == null)
        {
            config = new SyncConfiguration();
            await _configRepo.AddAsync(config);
            await _unitOfWork.SaveChangesAsync();
        }

        return Ok(new ConfigurationDto(
            CronExpression: config.CronExpression,
            IsEnabled: config.IsEnabled,
            DownloadAttachments: config.DownloadAttachments,
            GenerateThumbnails: config.GenerateThumbnails,
            ThumbnailMaxWidth: config.ThumbnailMaxWidth,
            AttachmentBasePath: config.AttachmentBasePath,
            LastSuccessfulSyncAt: config.LastSuccessfulSyncAt
        ));
    }

    /// <summary>
    /// Ayarları günceller
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ConfigurationDto>> UpdateConfiguration([FromBody] UpdateConfigurationRequest request)
    {
        var config = await _configRepo.Query().FirstOrDefaultAsync();
        
        if (config == null)
        {
            config = new SyncConfiguration();
            await _configRepo.AddAsync(config);
        }

        // Alanları güncelle
        if (request.CronExpression != null)
            config.CronExpression = request.CronExpression;
        
        if (request.IsEnabled.HasValue)
            config.IsEnabled = request.IsEnabled.Value;
        
        if (request.DownloadAttachments.HasValue)
            config.DownloadAttachments = request.DownloadAttachments.Value;
        
        if (request.GenerateThumbnails.HasValue)
            config.GenerateThumbnails = request.GenerateThumbnails.Value;
        
        if (request.ThumbnailMaxWidth.HasValue)
            config.ThumbnailMaxWidth = request.ThumbnailMaxWidth.Value;
        
        if (request.AttachmentBasePath != null)
            config.AttachmentBasePath = request.AttachmentBasePath;

        config.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        // Recurring job'u güncelle
        UpdateRecurringJob(config);

        _logger.LogInformation("⚙️ Configuration güncellendi. Enabled: {Enabled}, Cron: {Cron}", 
            config.IsEnabled, config.CronExpression);

        return Ok(new ConfigurationDto(
            CronExpression: config.CronExpression,
            IsEnabled: config.IsEnabled,
            DownloadAttachments: config.DownloadAttachments,
            GenerateThumbnails: config.GenerateThumbnails,
            ThumbnailMaxWidth: config.ThumbnailMaxWidth,
            AttachmentBasePath: config.AttachmentBasePath,
            LastSuccessfulSyncAt: config.LastSuccessfulSyncAt
        ));
    }

    /// <summary>
    /// Cron expression'ları döndürür (UI için yardımcı)
    /// </summary>
    [HttpGet("cron-presets")]
    public ActionResult<List<CronPreset>> GetCronPresets()
    {
        var presets = new List<CronPreset>
        {
            new("Her 1 saat", "0 * * * *"),
            new("Her 2 saat", "0 */2 * * *"),
            new("Her 3 saat", "0 */3 * * *"),
            new("Her 6 saat", "0 */6 * * *"),
            new("Her 12 saat", "0 */12 * * *"),
            new("Günde 1 kez (gece yarısı)", "0 0 * * *"),
            new("Günde 1 kez (sabah 6)", "0 6 * * *"),
            new("Günde 2 kez (6 ve 18)", "0 6,18 * * *"),
            new("Haftalık (Pazartesi 00:00)", "0 0 * * 1"),
        };

        return Ok(presets);
    }

    private void UpdateRecurringJob(SyncConfiguration config)
    {
        if (config.IsEnabled)
        {
            RecurringJob.AddOrUpdate<SyncJob>(
                "full-sync",
                job => job.ExecuteAsync(CancellationToken.None),
                config.CronExpression,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Local
                });
            
            _logger.LogInformation("📅 Recurring job aktive edildi: {Cron}", config.CronExpression);
        }
        else
        {
            RecurringJob.RemoveIfExists("full-sync");
            _logger.LogInformation("⏸️ Recurring job devre dışı bırakıldı");
        }
    }

    public record CronPreset(string Label, string Value);
}
