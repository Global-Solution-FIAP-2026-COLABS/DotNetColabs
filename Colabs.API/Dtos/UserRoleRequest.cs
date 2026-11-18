using System.ComponentModel.DataAnnotations;
using Colabs.Models;

namespace Colabs.Dtos;

/// <summary>Novo papel do usuário (User ou Admin).</summary>
public record class UserRoleRequest(
    [EnumDataType(typeof(UserRole), ErrorMessage = "Papel inválido. Use User ou Admin.")]
    UserRole Role
);