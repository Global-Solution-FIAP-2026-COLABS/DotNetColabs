using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;
using Colabs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colabs.Controllers
{
    /// <summary>
    /// Pedidos de ajuda. Qualquer usuário autenticado consulta e cria; editar, mudar o status e
    /// excluir é permitido somente ao dono do pedido ou a um administrador.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public class HelpRequestController : ControllerBase
    {
        private readonly IHelpRequestService _helpRequestService;

        public HelpRequestController(IHelpRequestService helpRequestService)
        {
            _helpRequestService = helpRequestService;
        }

        /// <summary>
        /// Lista os pedidos de ajuda com paginação, ordenação, filtros e links HATEOAS.
        /// </summary>
        /// <remarks>
        /// Padrão: 10 itens por página (máximo 50), ordenado por data de criação (use <c>ascending=false</c> para os mais recentes).
        /// Ordenações aceitas (<c>sortBy</c>): creationDate, status, id.
        /// Filtros: status (Open, InProgress, Completed, Canceled), idCategory, idUser e description (contém o texto).
        /// Exemplo: <c>/api/HelpRequest?status=Open&amp;idCategory=1&amp;ascending=false</c>.
        /// </remarks>
        /// <param name="query">Paginação, ordenação e filtros.</param>
        /// <response code="200">Página de pedidos retornada (pode ser vazia).</response>
        [HttpGet(Name = "GetHelpRequests")]
        [ProducesResponseType(typeof(PagedResponse<HelpRequestResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] HelpRequestQueryParameters query)
        {
            var result = await _helpRequestService.GetAllAsync(query);

            if (!result.IsSuccess)
                return this.ToErrorResult(result);

            var page = result.Value!.Map(ToResponse);

            var filters = new Dictionary<string, object?>
            {
                ["status"] = query.Status,
                ["idCategory"] = query.IdCategory,
                ["idUser"] = query.IdUser,
                ["description"] = query.Description
            };

            var links = PaginationLinkBuilder.Build(
                v => Url.Link("GetHelpRequests", v), query, page.TotalPages, filters);

            return Ok(PagedResponse<HelpRequestResponse>.From(page, links));
        }

        /// <summary>
        /// Obtém um pedido de ajuda pelo identificador.
        /// </summary>
        /// <param name="id">Identificador do pedido.</param>
        /// <response code="200">Pedido encontrado.</response>
        /// <response code="404">Pedido não encontrado.</response>
        [HttpGet("{id}", Name = "GetHelpRequestById")]
        [ProducesResponseType(typeof(HelpRequestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _helpRequestService.GetByIdAsync(id);

            return result.IsSuccess ? Ok(ToResponse(result.Value!)) : this.ToErrorResult(result);
        }

        /// <summary>
        /// Cria um pedido de ajuda em nome do usuário autenticado.
        /// </summary>
        /// <remarks>O pedido nasce com status Open e precisa de ao menos um item. A instituição é opcional.</remarks>
        /// <param name="request">Categoria, descrição e itens do pedido.</param>
        /// <response code="201">Pedido criado.</response>
        /// <response code="400">Dados inválidos (campo ausente, categoria ou instituição inexistente).</response>
        [HttpPost]
        [ProducesResponseType(typeof(HelpRequestResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] HelpRequestRequest request)
        {
            var result = await _helpRequestService.CreateAsync(request, User.GetRequester());

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ToResponse(result.Value!))
                : this.ToErrorResult(result);
        }

        /// <summary>
        /// Atualiza um pedido de ajuda (dono ou administrador) enquanto ele está Open.
        /// </summary>
        /// <remarks>A lista de itens enviada substitui a lista atual.</remarks>
        /// <param name="id">Identificador do pedido.</param>
        /// <param name="request">Novos dados do pedido.</param>
        /// <response code="204">Pedido atualizado.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="403">O usuário não é o dono do pedido.</response>
        /// <response code="404">Pedido não encontrado.</response>
        /// <response code="409">O pedido não está Open.</response>
        [HttpPut("{id}", Name = "UpdateHelpRequest")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, [FromBody] HelpRequestRequest request)
        {
            var result = await _helpRequestService.UpdateAsync(id, request, User.GetRequester());

            return result.IsSuccess ? NoContent() : this.ToErrorResult(result);
        }

        /// <summary>
        /// Muda o status de um pedido (dono ou administrador).
        /// </summary>
        /// <remarks>
        /// Transições permitidas: Open para InProgress ou Canceled; InProgress para Completed ou Canceled.
        /// Completed e Canceled registram a data de conclusão.
        /// </remarks>
        /// <param name="id">Identificador do pedido.</param>
        /// <param name="request">Novo status.</param>
        /// <response code="200">Status alterado. Retorna o pedido.</response>
        /// <response code="400">Status inválido.</response>
        /// <response code="403">O usuário não é o dono do pedido.</response>
        /// <response code="404">Pedido não encontrado.</response>
        /// <response code="409">Transição de status não permitida.</response>
        [HttpPut("{id}/status", Name = "ChangeHelpRequestStatus")]
        [ProducesResponseType(typeof(HelpRequestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] HelpRequestStatusRequest request)
        {
            var result = await _helpRequestService.ChangeStatusAsync(id, request.Status, User.GetRequester());

            return result.IsSuccess ? Ok(ToResponse(result.Value!)) : this.ToErrorResult(result);
        }

        /// <summary>
        /// Remove um pedido de ajuda (dono ou administrador).
        /// </summary>
        /// <remarks>Pedidos com doações vinculadas não podem ser removidos; cancele-os.</remarks>
        /// <param name="id">Identificador do pedido.</param>
        /// <response code="204">Pedido removido.</response>
        /// <response code="403">O usuário não é o dono do pedido.</response>
        /// <response code="404">Pedido não encontrado.</response>
        /// <response code="409">O pedido possui doações vinculadas.</response>
        [HttpDelete("{id}", Name = "DeleteHelpRequest")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _helpRequestService.DeleteAsync(id, User.GetRequester());

            return result.IsSuccess ? NoContent() : this.ToErrorResult(result);
        }

        /// <summary>Converte a entidade em response, anexando os links HATEOAS permitidos ao usuário.</summary>
        private HelpRequestResponse ToResponse(HelpRequest helpRequest)
        {
            var links = new List<LinkDto>
            {
                new(Url.Link("GetHelpRequestById", new { id = helpRequest.Id })!, LinkRelations.Self, "GET")
            };

            var requester = User.GetRequester();
            if (requester.IsAdmin || requester.UserId == helpRequest.IdUser)
            {
                links.Add(new(Url.Link("UpdateHelpRequest", new { id = helpRequest.Id })!, "update_help_request", "PUT"));
                links.Add(new(Url.Link("ChangeHelpRequestStatus", new { id = helpRequest.Id })!, "change_help_request_status", "PUT"));
                links.Add(new(Url.Link("DeleteHelpRequest", new { id = helpRequest.Id })!, "delete_help_request", "DELETE"));
            }

            return HelpRequestResponse.FromEntity(helpRequest, links);
        }
    }
}