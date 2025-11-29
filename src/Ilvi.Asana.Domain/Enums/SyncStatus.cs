namespace Ilvi.Asana.Domain.Enums;

/// <summary>
/// Senkronizasyon durumu
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// Çalışıyor
    /// </summary>
    Running,

    /// <summary>
    /// Başarıyla tamamlandı
    /// </summary>
    Completed,

    /// <summary>
    /// Hata ile sonlandı
    /// </summary>
    Failed,

    /// <summary>
    /// İptal edildi
    /// </summary>
    Cancelled
}
