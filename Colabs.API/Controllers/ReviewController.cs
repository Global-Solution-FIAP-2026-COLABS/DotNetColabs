using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;
using Colabs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colabs.Controllers
{
    /// <summary>
    /// Avaliação das entregas. Qualquer usuário autenticado consulta; criar, editar e excluir é permitido
    /// a quem recebeu a doação (dono do pedido vinculado) ou a um administrador.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        /// <summary>
        /// Lista as avaliações com paginação, ordenação, filtros e links HATEOAS.
        /// </summary>
        /// <remarks>
        /// Padrão: 10 itens por página (máximo 50), ordenado por data da avaliação (use <c>ascending=false</c> para as mais recentes).
        /// Ordenações aceitas (<c>sortBy</c>): reviewDate, rating, id. Filtros: idDonation e rating.
        /// Exemplo: <c>/api/Review?rating=5&amp;ascending=false</c>.
        /// </remarks>
        /// <param name="query">Paginação, ordenação e filtros.</param>
        /// <response code="200">Página de avaliações retornada (pode ser vazia).</response>
        [HttpGet(Name = "GetReviews")]
        [ProducesResponseType(typeof(PagedResponse<ReviewResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] ReviewQueryParameters query)
        {
            var result = await _reviewService.GetAllAsync(query);

            if (!result.IsSuccess)
                return this.ToErrorResult(result);

            var page = result.Value!.Map(ToResponse);

            var filters = new Dictionary<string, object?>
            {
                ["idDonation"] = query.IdDonation,
                ["rating"] = query.Rating
            };

            var links = PaginationLinkBuilder.Build(
                v => Url.Link("GetReviews", v), query, page.TotalPages, filters);

            return Ok(PagedResponse<ReviewResponse>.From(page, links));
        }

        /// <summary>
        /// Obtém uma avaliação pelo identificador.
        /// </summary>
        /// <param name="id">Identificador da avaliação.</param>
        /// <response code="200">Avaliação encontrada.</response>
        /// <response code="404">Avaliação não encontrada.</response>
        [HttpGet("{id}", Name = "GetReviewById")]
        [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _reviewService.GetByIdAsync(id);

            return result.IsSuccess ? Ok(ToResponse(result.Value!)) : this.ToErrorResult(result);
        }

        /// <summary>
        /// Avalia uma doação entregue (quem recebeu a doação ou administrador).
        /// </summary>
        /// <remarks>Só doações Delivered podem ser avaliadas, e cada doação recebe uma única avaliação.</remarks>
        /// <param name="request">Doação, nota (1 a 5) e comentário opcional.</param>
        /// <response code="201">Avaliação criada.</response>
        /// <response code="400">Dados inválidos (nota fora de 1 a 5, por exemplo).</response>
        /// <response code="403">O usuário não recebeu esta doação.</response>
        /// <response code="404">Doação não encontrada.</response>
        /// <response code="409">A doação não foi entregue ou já foi avaliada.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] ReviewRequest request)
        {
            var result = await _reviewService.CreateAsync(request, User.GetRequester());

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ToResponse(result.Value!))
                : this.ToErrorResult(result);
        }

        /// <summary>
        /// Atualiza uma avaliação (quem recebeu a doação ou administrador).
        /// </summary>
        /// <param name="id">Identificador da avaliação.</param>
        /// <param name="request">Nova nota e comentário.</param>
        /// <response code="204">Avaliação atualizada.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="403">O usuário não pode alterar esta avaliação.</response>
        /// <response code="404">Avaliação não encontrada.</response>
        [HttpPut("{id}", Name = "UpdateReview")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] ReviewUpdateRequest request)
        {
            var result = await _reviewService.UpdateAsync(id, request, User.GetRequester());

            return result.IsSuccess ? NoContent() : this.ToErrorResult(result);
        }

        /// <summary>
        /// Remove uma avaliação (quem recebeu a doação ou administrador).
        /// </summary>
        /// <param name="id">Identificador da avaliação.</param>
        /// <response code="204">Avaliação removida.</response>
        /// <response code="403">O usuário não pode remover esta avaliação.</response>
        /// <response code="404">Avaliação não encontrada.</response>
        [HttpDelete("{id}", Name = "DeleteReview")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _reviewService.DeleteAsync(id, User.GetRequester());

            return result.IsSuccess ? NoContent() : this.ToErrorResult(result);
        }

        /// <summary>Converte a entidade em response, anexando os links HATEOAS permitidos ao usuário.</summary>
        private ReviewResponse ToResponse(Review review)
        {
            var links = new List<LinkDto>
            {
                new(Url.Link("GetReviewById", new { id = review.Id })!, LinkRelations.Self, "GET"),
                new(Url.Link("GetDonationById", new { id = review.IdDonation })!, "donation", "GET")
            };

            // Quem recebeu a doação é o dono do pedido vinculado (carregado junto com a avaliação).
            var requester = User.GetRequester();
            var recipientId = review.Donation?.HelpRequest?.IdUser;

            if (requester.IsAdmin || recipientId == requester.UserId)
            {
                links.Add(new(Url.Link("UpdateReview", new { id = review.Id })!, "update_review", "PUT"));
                links.Add(new(Url.Link("DeleteReview", new { id = review.Id })!, "delete_review", "DELETE"));
            }

            return ReviewResponse.FromEntity(review, links);
        }
    }
}