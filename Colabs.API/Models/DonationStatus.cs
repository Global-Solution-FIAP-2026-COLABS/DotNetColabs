namespace Colabs.Models;

/// <summary>
/// Situação de uma doação. Persistido como texto (AVAILABLE, MATCHED, DELIVERED, CANCELED).
/// </summary>
public enum DonationStatus
{
    Available,
    Matched,
    Delivered,
    Canceled
}