using Microsoft.Extensions.Logging;

namespace Ilvi.Asana.Infrastructure.AsanaApi;

/// <summary>
/// Asana API rate limiting yönetimi
/// Free: 150 req/min, Paid: 1500 req/min
/// </summary>
public class AsanaRateLimiter
{
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly int _requestsPerMinute;
    private readonly Queue<DateTime> _requestTimestamps = new();
    private readonly object _timestampLock = new();
    private readonly ILogger<AsanaRateLimiter>? _logger;

    public AsanaRateLimiter(int requestsPerMinute = 1400, int maxConcurrentRequests = 50, ILogger<AsanaRateLimiter>? logger = null)
    {
        _requestsPerMinute = requestsPerMinute;
        _concurrencySemaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
        _logger = logger;
    }

    /// <summary>
    /// Rate limiting uygulayarak bir işlem çalıştırır
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        await _concurrencySemaphore.WaitAsync(ct);
        try
        {
            await WaitIfNeededAsync(ct);
            RecordRequest();
            return await action();
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    /// <summary>
    /// Rate limiting uygulayarak bir işlem çalıştırır (void)
    /// </summary>
    public async Task ExecuteAsync(Func<Task> action, CancellationToken ct = default)
    {
        await _concurrencySemaphore.WaitAsync(ct);
        try
        {
            await WaitIfNeededAsync(ct);
            RecordRequest();
            await action();
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    private async Task WaitIfNeededAsync(CancellationToken ct)
    {
        TimeSpan waitTime;
        
        lock (_timestampLock)
        {
            // 1 dakikadan eski kayıtları temizle
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < oneMinuteAgo)
            {
                _requestTimestamps.Dequeue();
            }

            // Limit aşıldıysa bekle
            if (_requestTimestamps.Count >= _requestsPerMinute)
            {
                var oldestRequest = _requestTimestamps.Peek();
                waitTime = oldestRequest.AddMinutes(1) - DateTime.UtcNow;
                
                if (waitTime > TimeSpan.Zero)
                {
                    _logger?.LogDebug("Rate limit'e yaklaşıldı. {WaitMs}ms bekleniyor...", waitTime.TotalMilliseconds);
                }
            }
            else
            {
                waitTime = TimeSpan.Zero;
            }
        }

        if (waitTime > TimeSpan.Zero)
        {
            await Task.Delay(waitTime, ct);
        }
    }

    private void RecordRequest()
    {
        lock (_timestampLock)
        {
            _requestTimestamps.Enqueue(DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Mevcut dakikadaki istek sayısını döndürür
    /// </summary>
    public int GetCurrentRequestCount()
    {
        lock (_timestampLock)
        {
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            return _requestTimestamps.Count(t => t >= oneMinuteAgo);
        }
    }
}
