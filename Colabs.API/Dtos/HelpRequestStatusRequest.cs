using System.ComponentModel.DataAnnotations;
using Colabs.Models;

namespace Colabs.Dtos;

/// <summary>Novo status do pedido (Open, InProgress, Completed ou Canceled).</summary>
public record class HelpRequestStatusRequest(
    [EnumDataType(typeof(HelpRequestStatus), ErrorMessage = "Status inválido.")]
    HelpRequestStatus Status
);