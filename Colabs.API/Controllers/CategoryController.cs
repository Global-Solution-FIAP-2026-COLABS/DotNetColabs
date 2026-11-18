using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;
using Colabs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colabs.Controllers
{
    /// <summary>
    /// Categorias de pedidos de ajuda e de doações. Leitura para qualquer usuário autenticado;
    /// criação, edição e exclusão somente para administradores.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Lista as categorias com paginação, ordenação, filtro e links HATEOAS.
        /// </summary>
        /// <remarks>
        /// Padrão: 10 itens por página (máximo 50), ordenado por nome. Ordenações aceitas (<c>sortBy</c>): name, id.
        /// Filtro: name (contém o texto, sem diferenciar maiúsculas).
        /// Exemplo: <c>/api/Category?name=ali&amp;ascending=false&amp;pageSize=5</c>.
        /// </remarks>
        /// <param name="query">Paginação, ordenação e filtro.</param>
        /// <response code="200">Página de categorias retornada (pode ser vazia).</response>
        [HttpGet(Name = "GetCategories")]
        [ProducesResponseType(typeof(PagedResponse<CategoryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] CategoryQueryParameters query)
        {
            var result = await _categoryService.GetAllAsync(query);

            if (!result.IsSuccess)
                return this.ToErrorResult(result);

            var page = result.Value!.Map(ToResponse);

            var filters = new Dictionary<string, object?>
            {
                ["name"] = query.Name
            };

            var links = PaginationLinkBuilder.Build(
                v => Url.Link("GetCategories", v), query, page.TotalPages, filters);

            return Ok(PagedResponse<CategoryResponse>.From(page, links));
        }

        /// <summary>
        /// Obtém uma categoria pelo identificador.
        /// </summary>
        /// <param name="id">Identificador da categoria.</param>
        /// <response code="200">Categoria encontrada.</response>
        /// <response code="404">Categoria não encontrada.</response>
        [HttpGet("{id}", Name = "GetCategoryById")]
        [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _categoryService.GetByIdAsync(id);

            return result.IsSuccess ? Ok(ToResponse(result.Value!)) : this.ToErrorResult(result);
        }

        /// <summary>
        /// Cadastra uma nova categoria (somente administradores).
        /// </summary>
        /// <param name="request">Dados da categoria.</param>
        /// <response code="201">Categoria criada.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="403">O usuário não é administrador.</response>
        /// <response code="409">Já existe categoria com este nome.</response>
        [HttpPost]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CategoryRequest request)
        {
            var result = await _categoryService.CreateAsync(request);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ToResponse(result.Value!))
                : this.ToErrorResult(result);
        }

        /// <summary>
        /// Atualiza uma categoria (somente administradores).
        /// </summary>
        /// <param name="id">Identificador da categoria.</param>
        /// <param name="request">Novo nome.</param>
        /// <response code="204">Categoria atualizada.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="403">O usuário não é administrador.</response>
        /// <response code="404">Categoria não encontrada.</response>
        /// <response code="409">Já existe outra categoria com este nome.</response>
        [HttpPut("{id}", Name = "UpdateCategory")]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
        {
            var result = await _categoryService.UpdateAsync(id, request);

            return result.IsSuccess ? NoContent() : this.ToErrorResult(result);
        }

        /// <summary>
        /// Remove uma categoria (somente administradores).
        /// </summary>
        /// <param name="id">Identificador da categoria.</param>
        /// <response code="204">Categoria removida.</response>
        /// <response code="403">O usuário não é administrador.</response>
        /// <response code="404">Categoria não encontrada.</response>
        /// <response code="409">A categoria está em uso por pedidos ou doações.</response>
        [HttpDelete("{id}", Name = "DeleteCategory")]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _categoryService.DeleteAsync(id);

            return result.IsSuccess ? NoContent() : this.ToErrorResult(result);
        }

        /// <summary>Converte a entidade em response, anexando os links HATEOAS permitidos ao usuário.</summary>
        private CategoryResponse ToResponse(Category category)
        {
            var links = new List<LinkDto>
            {
                new(Url.Link("GetCategoryById", new { id = category.Id })!, LinkRelations.Self, "GET")
            };

            if (User.IsAdmin())
            {
                links.Add(new(Url.Link("UpdateCategory", new { id = category.Id })!, "update_category", "PUT"));
                links.Add(new(Url.Link("DeleteCategory", new { id = category.Id })!, "delete_category", "DELETE"));
            }

            return CategoryResponse.FromEntity(category, links);
        }
    }
}