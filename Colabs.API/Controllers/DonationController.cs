using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;
using Colabs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colabs.Controllers
{
    /// <summary>
    /// Doações disponíveis. Qualquer usuário autenticado consulta e cadastra; editar, mudar o status e
    /// excluir é permitido ao doador ou a um administrador.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public class DonationController : ControllerBase
    {
        private readonly IDonationService _donationService;

        public DonationController(IDonationService donationService)
        {
            _donationService = donationService;
        }

        /// <summary>
        /// Lista as doações com paginação, ordenação, filtros e links HATEOAS.
        /// </summary>
        /// <remarks>
        /// Padrão: 10 itens por página (máximo 50), ordenado por data de criação (use <c>ascending=false</c> para as mais recentes).
        /// Ordenações aceitas (<c>sortBy</c>): creationDate, status, id.
        /// Filtros: status (Available, Matched, Delivered, Canceled), idCategory, idUser, idHelpRequest e description (contém o texto).
        /// Exemplo: <c>/api/Donation?status=Available&amp;idCategory=1&amp;ascending=false</c>.
        /// </remarks>
        /// <param name="query">Paginação, ordenação e filtros.</param>
        /// <response code="200">Página de doações retornada (pode ser vazia).</response>
        [HttpGet(Name = "GetDonations")]
        [ProducesResponseType(typeof(PagedResponse<DonationResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] DonationQueryParameters query)
        {
            var result = await _donationService.GetAllAsync(query);

            if (!result.IsSuccess)
                return this.ToErrorResult(result);

            var page = result.Value!.Map(ToResponse);

            var filters = new Dictionary<string, object?>
            {
                ["status"] = query.Status,
                ["idCategory"] = query.IdCategory,
                ["idUser"] = query.IdUser,
                ["idHelpRequest"] = query.IdHelpRequest,
                ["description"] = query.Description
            };

            var links = PaginationLinkBuilder.Build(
                v => Url.Link("GetDonations", v), query, page.TotalPages, filters);

            return Ok(PagedResponse<DonationResponse>.From(page, links));
        }

        /// <summary>
        /// Obtém uma doação pelo identificador.
        /// </summary>
        /// <param name="id">Identificador da doação.</param>
        /// <response code="200">Doação encontrada.</response>
        /// <response code="404">Doação não encontrada.</response>
        [HttpGet("{id}", Name = "GetDonationById")]
        [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _donationService.GetByIdAsync(id);

            return result.IsSuccess ? Ok(ToResponse(result.Value!)) : this.ToErrorResult(result);
        }

        /// <summary>
        /// Cadastra uma doação em nome do usuário autenticado.
        /// </summary>
        /// <remarks>A doação nasce com status Available e precisa de ao menos um item. A instituição é opcional.</remarks>
        /// <param name="request">Categoria, descrição e itens da doação.</param>
        /// <response code="201">Doação criada.</response>
        /// <response code="400">Dados inválidos (campo ausente, categoria ou instituição inexistente).</response>
        [HttpPost]
        [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] DonationRequest request)
        {
            var result = await _donationService.CreateAsync(request, User.GetRequester());

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ToResponse(result.Value!))
                : this.ToErrorResult(result);
        }

        /// <summary>
        /// Atualiza uma doação (doador ou administrador) enquanto ela está Available.
        /// </summary>
        /// <remarks>A lista de itens enviada substitui a lista atual.</remarks>
        /// <param name="id">Identificador da doação.</param>
        /// <param name="request">Novos dados da doação.</param>
        /// <response code="204">Doação atualizada.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="403">O usuário não é o doador.</response>
        /// <response code="404">Doação não encontrada.</response>
        /// <response code="409">A doação não está Available.</response>
        [HttpPut("{id}", Name = "UpdateDonation")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, [FromBody] DonationRequest request)
        {
            var result = await _donationService.UpdateAsync(id, request, User.GetRequester());

            return result.IsSuccess ? NoContent() : this.ToErrorResult(result);
        }

        /// <summary>
        /// Muda o status de uma doação.
        /// </summary>
        /// <remarks>
        /// Transições manuais: Available para Canceled; Matched para Delivered ou Canceled.
        /// Available para Matched acontece somente ao efetivar o match de um pedido.
        /// O doador e o administrador fazem qualquer transição; o dono do pedido vinculado também pode marcar Delivered.
        /// Delivered conclui o pedido vinculado; Canceled devolve o pedido para Open se não restar outra doação ativa.
        /// </remarks>
        /// <param name="id">Identificador da doação.</param>
        /// <param name="request">Novo status.</param>
        /// <response code="200">Status alterado. Retorna a doação.</response>
        /// <response code="400">Status inválido.</response>
        /// <response code="403">O usuário não pode alterar esta doação.</response>
        /// <response code="404">Doação não encontrada.</response>
        /// <response code="409">Transição de status não permitida.</response>
        [HttpPut("{id}/status", Name = "ChangeDonationStatus")]
        [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] DonationStatusRequest request)
        {
            var result = await _donationService.ChangeStatusAsync(id, request.Status, User.GetRequester());

            return result.IsSuccess ? Ok(ToResponse(result.Value!)) : this.ToErrorResult(result);
        }

        /// <summary>
        /// Remove uma doação (doador ou administrador).
        /// </summary>
        /// <remarks>Doações Matched ou Delivered não podem ser removidas.</remarks>
        /// <param name="id">Identificador da doação.</param>
        /// <response code="204">Doação removida.</response>
        /// <response code="403">O usuário não é o doador.</response>
        /// <response code="404">Doação não encontrada.</response>
        /// <response code="409">A doação está Matched ou Delivered.</response>
        [HttpDelete("{id}", Name = "DeleteDonation")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _donationService.DeleteAsync(id, User.GetRequester());

            return result.IsSuccess ? NoContent() : this.ToErrorResult(result);
        }

        /// <summary>Converte a entidade em response, anexando os links HATEOAS permitidos ao usuário.</summary>
        private DonationResponse ToResponse(Donation donation)
        {
            var links = new List<LinkDto>
            {
                new(Url.Link("GetDonationById", new { id = donation.Id })!, LinkRelations.Self, "GET")
            };

            var requester = User.GetRequester();
            if (requester.IsAdmin || requester.UserId == donation.IdUser)
            {
                links.Add(new(Url.Link("UpdateDonation", new { id = donation.Id })!, "update_donation", "PUT"));
                links.Add(new(Url.Link("ChangeDonationStatus", new { id = donation.Id })!, "change_donation_status", "PUT"));
                links.Add(new(Url.Link("DeleteDonation", new { id = donation.Id })!, "delete_donation", "DELETE"));
            }

            return DonationResponse.FromEntity(donation, links);
        }
    }
}