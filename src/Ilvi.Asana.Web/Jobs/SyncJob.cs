using Hangfire;
using Ilvi.Asana.Domain.Interfaces;

namespace Ilvi.Asana.Web.Jobs;

/// <summary>
/// Hangfire sync job
/// </summary>
public class SyncJob
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncJob> _logger;

    public SyncJob(ISyncService syncService, ILogger<SyncJob> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    [Queue("sync")]
    [AutomaticRetry(Attempts = 0)] // Sync hatalarında otomatik retry yapma
    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("🔄 Scheduled sync job başlatılıyor...");
        
        try
        {
            var result = await _syncService.ExecuteFullSyncAsync(ct);
            _logger.LogInformation("✅ Scheduled sync tamamlandı. Süre: {Duration}s", result.DurationSeconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⚠️ Sync job iptal edildi");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Sync job başarısız");
            throw;
        }
    }
}
