using Colabs.Common;
using Colabs.Models;

namespace Colabs.Dtos;

public record class InstitutionResponse(
    int Id,
    string Name,
    string Cnpj,
    string Address,
    decimal Latitude,
    decimal Longitude,
    List<LinkDto> Links
)
{
    public static InstitutionResponse FromEntity(Institution institution, List<LinkDto> links) => new(
        institution.Id, institution.Name, institution.Cnpj, institution.Address,
        institution.Latitude, institution.Longitude, links);
}