namespace Ilvi.Asana.Domain.Interfaces;

/// <summary>
/// Dosya depolama servisi interface
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Dosya indirir ve kaydeder
    /// </summary>
    /// <param name="url">İndirilecek URL</param>
    /// <param name="fileName">Dosya adı</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Kaydedilen dosya yolları (original, thumbnail)</returns>
    Task<StorageResult> DownloadAndSaveAsync(string url, string fileName, CancellationToken ct = default);

    /// <summary>
    /// Dosya siler
    /// </summary>
    Task DeleteAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Dosya var mı kontrol eder
    /// </summary>
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Dosya boyutunu döndürür
    /// </summary>
    Task<long?> GetFileSizeAsync(string path, CancellationToken ct = default);
}

public record StorageResult(
    string OriginalPath,
    string? ThumbnailPath,
    long FileSize,
    bool Success,
    string? ErrorMessage = null
);
