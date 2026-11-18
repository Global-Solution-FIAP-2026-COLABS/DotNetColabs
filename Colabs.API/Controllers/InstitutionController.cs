using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;
using Colabs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colabs.Controllers
{
    /// <summary>
    /// Instituições e organizações que atuam como pontos de apoio. Leitura para qualquer usuário autenticado;
    /// criação, edição e exclusão somente para administradores.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public class InstitutionController : ControllerBase
    {
        private readonly IInstitutionService _institutionService;

        public InstitutionController(IInstitutionService institutionService)
        {
            _institutionService = institutionService;
        }

        /// <summary>
        /// Lista as instituições com paginação, ordenação, filtros e links HATEOAS.
        /// </summary>
        /// <remarks>
        /// Padrão: 10 itens por página (máximo 50), ordenado por nome. Ordenações aceitas (<c>sortBy</c>): name, cnpj, address.
        /// Filtros: name e address (contém o texto, sem diferenciar maiúsculas) e cnpj (contém, com ou sem pontuação).
        /// Exemplo: <c>/api/Institution?name=casa&amp;sortBy=cnpj&amp;pageSize=5</c>.
        /// </remarks>
        /// <param name="query">Paginação, ordenação e filtros.</param>
        /// <response code="200">Página de instituições retornada (pode ser vazia).</response>
        [HttpGet(Name = "GetInstitutions")]
        [ProducesResponseType(typeof(PagedResponse<InstitutionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] InstitutionQueryParameters query)
        {
            var result = await _institutionService.GetAllAsync(query);

            if (!result.IsSuccess)
                return this.ToErrorResult(result);

            var page = result.Value!.Map(ToResponse);

            var filters = new Dictionary<string, object?>
            {
                ["name"] = query.Name,
                ["cnpj"] = query.Cnpj,
                ["address"] = query.Address
            };

            var links = PaginationLinkBuilder.Build(
                v => Url.Link("GetInstitutions", v), query, page.TotalPages, filters);

            return Ok(PagedResponse<InstitutionResponse>.From(page, links));
        }

        /// <summary>
        /// Obtém uma instituição pelo identificador.
        /// </summary>
        /// <param name="id">Identificador da instituição.</param>
        /// <response code="200">Instituição encontrada.</response>
        /// <response code="404">Instituição não encontrada.</response>
        [HttpGet("{id}", Name = "GetInstitutionById")]
        [ProducesResponseType(typeof(InstitutionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _institutionService.GetByIdAsync(id);

            return result.IsSuccess ? Ok(ToResponse(result.Value!)) : this.ToErrorResult(result);
        }

        /// <summary>
        /// Cadastra uma nova instituição (somente administradores).
        /// </summary>
        /// <remarks>
        /// O CNPJ aceita pontuação e é armazenado só com dígitos. Latitude e longitude são obrigatórias,
        /// pois definem o ponto da instituição no mapa.
        /// </remarks>
        /// <param name="request">Dados da instituição.</param>
        /// <response code="201">Instituição criada.</response>
        /// <response code="400">Dados inválidos (CNPJ inválido, coordenadas ausentes ou fora da faixa etc.).</response>
        /// <response code="403">O usuário não é administrador.</response>
        /// <response code="409">Já existe instituição com este CNPJ.</response>
        [HttpPost]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        [ProducesResponseType(typeof(InstitutionResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] InstitutionRequest request)
        {
            var result = await _institutionService.CreateAsync(request);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ToResponse(result.Value!))
                : this.ToErrorResult(result);
        }

        /// <summary>
        /// Atualiza os dados de uma instituição (somente administradores). O CNPJ não é alterado.
        /// </summary>
        /// <param name="id">Identificador da instituição.</param>
        /// <param name="request">Novos dados.</param>
        /// <response code="204">Instituição atualizada.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="403">O usuário não é administrador.</response>
        /// <response code="404">Instituição não encontrada.</response>
        [HttpPut("{id}", Name = "UpdateInstitution")]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] InstitutionUpdateRequest request)
        {
            var result = await _institutionService.UpdateAsync(id, request);

            return result.IsSuccess ? NoContent() : this.ToErrorResult(result);
        }

        /// <summary>
        /// Remove uma instituição (somente administradores).
        /// </summary>
        /// <param name="id">Identificador da instituição.</param>
        /// <response code="204">Instituição removida.</response>
        /// <response code="403">O usuário não é administrador.</response>
        /// <response code="404">Instituição não encontrada.</response>
        /// <response code="409">A instituição possui pedidos ou doações vinculados.</response>
        [HttpDelete("{id}", Name = "DeleteInstitution")]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _institutionService.DeleteAsync(id);

            return result.IsSuccess ? NoContent() : this.ToErrorResult(result);
        }

        /// <summary>Converte a entidade em response, anexando os links HATEOAS permitidos ao usuário.</summary>
        private InstitutionResponse ToResponse(Institution institution)
        {
            var links = new List<LinkDto>
            {
                new(Url.Link("GetInstitutionById", new { id = institution.Id })!, LinkRelations.Self, "GET")
            };

            if (User.IsAdmin())
            {
                links.Add(new(Url.Link("UpdateInstitution", new { id = institution.Id })!, "update_institution", "PUT"));
                links.Add(new(Url.Link("DeleteInstitution", new { id = institution.Id })!, "delete_institution", "DELETE"));
            }

            return InstitutionResponse.FromEntity(institution, links);
        }
    }
}