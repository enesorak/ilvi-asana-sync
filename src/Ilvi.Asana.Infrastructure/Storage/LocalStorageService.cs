using Ilvi.Asana.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Ilvi.Asana.Infrastructure.Storage;

/// <summary>
/// Yerel dosya sistemi depolama servisi
/// Orijinal dosya + thumbnail oluşturma desteği
/// </summary>
public class LocalStorageService : IStorageService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly string _basePath;
    private readonly int _thumbnailMaxWidth;
    private readonly bool _generateThumbnails;

    // Thumbnail oluşturulabilecek uzantılar
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    };

    public LocalStorageService(
        HttpClient httpClient,
        ILogger<LocalStorageService> logger,
        string basePath = "./attachments",
        int thumbnailMaxWidth = 400,
        bool generateThumbnails = true)
    {
        _httpClient = httpClient;
        _logger = logger;
        _basePath = basePath;
        _thumbnailMaxWidth = thumbnailMaxWidth;
        _generateThumbnails = generateThumbnails;

        // Klasörleri oluştur
        Directory.CreateDirectory(Path.Combine(_basePath, "original"));
        Directory.CreateDirectory(Path.Combine(_basePath, "thumbnails"));
    }

    public async Task<StorageResult> DownloadAndSaveAsync(string url, string fileName, CancellationToken ct = default)
    {
        try
        {
            // Dosyayı indir
            _logger.LogDebug("İndiriliyor: {FileName}", fileName);
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsByteArrayAsync(ct);
            var fileSize = content.Length;

            // Dosya adını temizle ve benzersiz yap
            var safeFileName = SanitizeFileName(fileName);
            var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
            var nameWithoutExt = Path.GetFileNameWithoutExtension(safeFileName);
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var finalFileName = $"{nameWithoutExt}_{uniqueId}{extension}";

            // Orijinal dosyayı kaydet
            var originalPath = Path.Combine(_basePath, "original", finalFileName);
            await File.WriteAllBytesAsync(originalPath, content, ct);

            // Thumbnail oluştur (eğer resim ise)
            string? thumbnailPath = null;
            if (_generateThumbnails && IsImageFile(extension))
            {
                try
                {
                    thumbnailPath = await CreateThumbnailAsync(content, finalFileName, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Thumbnail oluşturulamadı: {FileName}", fileName);
                    // Thumbnail hatası ana işlemi engellemez
                }
            }

            _logger.LogDebug("İndirildi: {FileName} ({Size} bytes)", fileName, fileSize);

            return new StorageResult(
                OriginalPath: originalPath,
                ThumbnailPath: thumbnailPath,
                FileSize: fileSize,
                Success: true
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Dosya indirilemedi: {FileName} - {Url}", fileName, url);
            return new StorageResult(
                OriginalPath: "",
                ThumbnailPath: null,
                FileSize: 0,
                Success: false,
                ErrorMessage: $"HTTP Error: {ex.Message}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya kaydedilemedi: {FileName}", fileName);
            return new StorageResult(
                OriginalPath: "",
                ThumbnailPath: null,
                FileSize: 0,
                Success: false,
                ErrorMessage: ex.Message
            );
        }
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        return Task.FromResult(File.Exists(path));
    }

    public Task<long?> GetFileSizeAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
            return Task.FromResult<long?>(null);

        var info = new FileInfo(path);
        return Task.FromResult<long?>(info.Length);
    }

    #region Private Methods

    private async Task<string> CreateThumbnailAsync(byte[] imageData, string fileName, CancellationToken ct)
    {
        using var image = Image.Load(imageData);
        
        // Boyut hesapla (oranı koruyarak)
        var ratio = (double)_thumbnailMaxWidth / image.Width;
        var newHeight = (int)(image.Height * ratio);

        // Zaten küçükse thumbnail oluşturma
        if (image.Width <= _thumbnailMaxWidth)
        {
            var smallPath = Path.Combine(_basePath, "thumbnails", fileName);
            await File.WriteAllBytesAsync(smallPath, imageData, ct);
            return smallPath;
        }

        // Resize et
        image.Mutate(x => x.Resize(_thumbnailMaxWidth, newHeight));

        // Kaydet
        var thumbnailPath = Path.Combine(_basePath, "thumbnails", fileName);
        await image.SaveAsync(thumbnailPath, ct);

        return thumbnailPath;
    }

    private static bool IsImageFile(string extension)
    {
        return ImageExtensions.Contains(extension);
    }

    private static string SanitizeFileName(string fileName)
    {
        // Geçersiz karakterleri temizle
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(c => !invalidChars.Contains(c))
            .ToArray());

        // Boşlukları alt çizgi ile değiştir
        sanitized = sanitized.Replace(' ', '_');

        // Maksimum uzunluk
        if (sanitized.Length > 200)
        {
            var ext = Path.GetExtension(sanitized);
            var name = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = name[..(200 - ext.Length)] + ext;
        }

        return sanitized;
    }

    #endregion
}
