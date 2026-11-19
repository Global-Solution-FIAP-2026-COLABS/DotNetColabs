using System.ComponentModel.DataAnnotations;
using Colabs.Models;

namespace Colabs.Dtos;

/// <summary>Novo status da doação (Available, Matched, Delivered ou Canceled).</summary>
public record class DonationStatusRequest(
    [EnumDataType(typeof(DonationStatus), ErrorMessage = "Status inválido.")]
    DonationStatus Status
);