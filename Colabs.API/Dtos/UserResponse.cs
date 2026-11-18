using Colabs.Common;
using Colabs.Models;

namespace Colabs.Dtos;

/// <summary>Dados do usuário devolvidos pela API. A senha (nem o hash) nunca é retornada.</summary>
public record class UserResponse(
    int Id,
    string Name,
    string Cpf,
    string Phone,
    string Email,
    string Address,
    decimal? Latitude,
    decimal? Longitude,
    string Role,
    List<LinkDto> Links
)
{
    public static UserResponse FromEntity(User user, List<LinkDto>? links = null)
    {
        return new UserResponse(
            user.Id,
            user.Name,
            user.Cpf,
            user.Phone,
            user.Email,
            user.Address,
            user.Latitude,
            user.Longitude,
            AuthRoles.From(user.Role),
            links ?? new List<LinkDto>()
        );
    }
}