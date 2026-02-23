namespace Infrastructure.Messaging;

/// <summary>
/// Sistematik hata durumlarinda receive endpoint'i gecici olarak durduran kill-switch ayarlari.
/// </summary>
public sealed class KillSwitchOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Tracking window (or. 60s).
    /// </summary>
    public int TrackingPeriodSeconds { get; set; } = 60;

    /// <summary>
    /// Kill-switch'i aktive etmek icin minimum mesaj sayisi.
    /// </summary>
    public int ActivationThreshold { get; set; } = 20;

    /// <summary>
    /// Tracking window icinde hata orani bu degeri gecerse endpoint durdurulur (0-100).
    /// </summary>
    public int TripThreshold { get; set; } = 50;

    /// <summary>
    /// Endpoint'in yeniden baslatilmasi icin beklenecek sure.
    /// </summary>
    public int RestartTimeoutSeconds { get; set; } = 60;
}
