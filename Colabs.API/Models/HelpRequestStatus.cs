namespace Colabs.Models;

/// <summary>
/// Situação de um pedido de ajuda. Persistido como texto (OPEN, IN_PROGRESS, COMPLETED, CANCELED).
/// </summary>
public enum HelpRequestStatus
{
    Open,
    InProgress,
    Completed,
    Canceled
}