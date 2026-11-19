using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;
using Colabs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colabs.Controllers
{
    /// <summary>
    /// Histórico de doações do usuário autenticado, como doador e como quem recebeu.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public class HistoryController : ControllerBase
    {
        private readonly IHistoryService _historyService;

        public HistoryController(IHistoryService historyService)
        {
            _historyService = historyService;
        }

        /// <summary>
        /// Lista o histórico de doações do usuário, com paginação, ordenação, filtros e links HATEOAS.
        /// </summary>
        /// <remarks>
        /// Inclui as doações em que o usuário foi o doador (<c>Donor</c>) e aquelas ligadas a pedidos dele (<c>Recipient</c>).
        /// Cada item traz o papel, o status e a nota da avaliação (quando houver).
        /// Padrão: 10 itens por página (máximo 50), ordenado por data de criação (use <c>ascending=false</c> para as mais recentes).
        /// Ordenações aceitas (<c>sortBy</c>): creationDate, status, id.
        /// Filtros: role (Donor ou Recipient), status, from e to (datas de criação, inclusive) e idUser (somente administradores).
        /// Exemplo: <c>/api/History/donations?role=Donor&amp;status=Delivered&amp;from=2026-01-01&amp;ascending=false</c>.
        /// </remarks>
        /// <param name="query">Paginação, ordenação e filtros.</param>
        /// <response code="200">Página do histórico retornada (pode ser vazia).</response>
        /// <response code="403">Usuário comum tentando ver o histórico de outro usuário.</response>
        [HttpGet("donations", Name = "GetDonationHistory")]
        [ProducesResponseType(typeof(PagedResponse<HistoryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDonations([FromQuery] HistoryQueryParameters query)
        {
            var requester = User.GetRequester();
            var result = await _historyService.GetDonationsAsync(query, requester);

            if (!result.IsSuccess)
                return this.ToErrorResult(result);

            var userId = query.IdUser ?? requester.UserId;
            var page = result.Value!.Map(d => ToResponse(d, userId));

            var filters = new Dictionary<string, object?>
            {
                ["role"] = query.Role,
                ["status"] = query.Status,
                ["from"] = query.From?.ToString("yyyy-MM-dd"),
                ["to"] = query.To?.ToString("yyyy-MM-dd"),
                ["idUser"] = query.IdUser
            };

            var links = PaginationLinkBuilder.Build(
                v => Url.Link("GetDonationHistory", v), query, page.TotalPages, filters);

            return Ok(PagedResponse<HistoryResponse>.From(page, links));
        }

        /// <summary>Converte a doação em item do histórico, com link para a própria doação.</summary>
        private HistoryResponse ToResponse(Donation donation, int userId)
        {
            var links = new List<LinkDto>
            {
                new(Url.Link("GetDonationById", new { id = donation.Id })!, "donation", "GET")
            };

            return HistoryResponse.FromEntity(donation, userId, links);
        }
    }
}