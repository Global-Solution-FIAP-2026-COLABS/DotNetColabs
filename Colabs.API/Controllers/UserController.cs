using Colabs.Common;
using Colabs.Dtos;
using Colabs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserEntity = Colabs.Models.User;

namespace Colabs.Controllers
{
    /// <summary>
    /// Gestão de usuários. O cadastro público é feito em <c>POST /api/Auth/register</c>.
    /// Consulta, edição e exclusão: somente o próprio usuário ou um administrador.
    /// Listagem e alteração de papel: somente administradores.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Lista os usuários com paginação, ordenação, filtros e links HATEOAS (somente administradores).
        /// </summary>
        /// <remarks>
        /// Padrão: 10 itens por página (máximo 50), ordenado por nome. Ordenações aceitas (<c>sortBy</c>): name, email, id.
        /// Filtros: name e email (contém o texto, sem diferenciar maiúsculas), cpf (contém, com ou sem pontuação)
        /// e role (User ou Admin). A senha nunca é retornada.
        /// Exemplo: <c>/api/User?role=Admin&amp;sortBy=email&amp;pageSize=5</c>.
        /// </remarks>
        /// <param name="query">Paginação, ordenação e filtros.</param>
        /// <response code="200">Página de usuários retornada (pode ser vazia).</response>
        /// <response code="403">O usuário não é administrador.</response>
        [HttpGet(Name = "GetUsers")]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        [ProducesResponseType(typeof(PagedResponse<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll([FromQuery] UserQueryParameters query)
        {
            var result = await _userService.GetAllAsync(query);

            if (!result.IsSuccess)
                return this.ToErrorResult(result);

            var page = result.Value!.Map(ToResponse);

            var filters = new Dictionary<string, object?>
            {
                ["name"] = query.Name,
                ["email"] = query.Email,
                ["cpf"] = query.Cpf,
                ["role"] = query.Role
            };

            var links = PaginationLinkBuilder.Build(
                v => Url.Link("GetUsers", v), query, page.TotalPages, filters);

            return Ok(PagedResponse<UserResponse>.From(page, links));
        }

        /// <summary>
        /// Obtém um usuário pelo identificador (o próprio usuário ou um administrador).
        /// </summary>
        /// <param name="id">Identificador do usuário.</param>
        /// <response code="200">Usuário encontrado.</response>
        /// <response code="403">O usuário autenticado não pode acessar este cadastro.</response>
        /// <response code="404">Usuário não encontrado.</response>
        [HttpGet("{id}", Name = "GetUserById")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _userService.GetByIdAsync(id, User.GetRequester());

            return result.IsSuccess ? Ok(ToResponse(result.Value!)) : this.ToErrorResult(result);
        }

        /// <summary>
        /// Atualiza os dados cadastrais de um usuário (o próprio usuário ou um administrador).
        /// </summary>
        /// <remarks>CPF, senha e papel não podem ser alterados aqui. O e-mail é armazenado em minúsculas.</remarks>
        /// <param name="id">Identificador do usuário.</param>
        /// <param name="request">Novos dados.</param>
        /// <response code="204">Usuário atualizado.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="403">O usuário autenticado não pode alterar este cadastro.</response>
        /// <response code="404">Usuário não encontrado.</response>
        /// <response code="409">O e-mail já pertence a outro usuário.</response>
        [HttpPut("{id}", Name = "UpdateUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, [FromBody] UserUpdateRequest request)
        {
            var result = await _userService.UpdateAsync(id, request, User.GetRequester());

            return result.IsSuccess ? NoContent() : this.ToErrorResult(result);
        }

        /// <summary>
        /// Remove um usuário (o próprio usuário ou um administrador).
        /// </summary>
        /// <remarks>
        /// Não é possível remover usuários com pedidos de ajuda ou doações vinculados, nem o último administrador.
        /// O token do usuário removido continua válido até expirar.
        /// </remarks>
        /// <param name="id">Identificador do usuário.</param>
        /// <response code="204">Usuário removido.</response>
        /// <response code="403">O usuário autenticado não pode remover este cadastro.</response>
        /// <response code="404">Usuário não encontrado.</response>
        /// <response code="409">Usuário com vínculos, ou último administrador.</response>
        [HttpDelete("{id}", Name = "DeleteUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _userService.DeleteAsync(id, User.GetRequester());

            return result.IsSuccess ? NoContent() : this.ToErrorResult(result);
        }

        /// <summary>
        /// Promove ou rebaixa o papel de um usuário (somente administradores).
        /// </summary>
        /// <remarks>
        /// O novo papel só vale no próximo login do usuário afetado (o papel vai dentro do token).
        /// Um administrador não pode rebaixar a si mesmo.
        /// </remarks>
        /// <param name="id">Identificador do usuário.</param>
        /// <param name="request">Novo papel: User ou Admin.</param>
        /// <response code="200">Papel alterado (ou já era o informado). Retorna o usuário.</response>
        /// <response code="400">Papel inválido.</response>
        /// <response code="403">O usuário não é administrador.</response>
        /// <response code="404">Usuário não encontrado.</response>
        /// <response code="409">Auto-rebaixamento ou último administrador.</response>
        [HttpPut("{id}/role", Name = "ChangeUserRole")]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] UserRoleRequest request)
        {
            var result = await _userService.ChangeRoleAsync(id, request.Role, User.GetRequester());

            return result.IsSuccess ? Ok(ToResponse(result.Value!)) : this.ToErrorResult(result);
        }

        /// <summary>Converte a entidade em response, anexando os links HATEOAS permitidos ao usuário autenticado.</summary>
        private UserResponse ToResponse(UserEntity user)
        {
            var requester = User.GetRequester();

            var links = new List<LinkDto>
            {
                new(Url.Link("GetUserById", new { id = user.Id })!, LinkRelations.Self, "GET")
            };

            if (requester.IsAdmin || requester.UserId == user.Id)
            {
                links.Add(new(Url.Link("UpdateUser", new { id = user.Id })!, "update_user", "PUT"));
                links.Add(new(Url.Link("DeleteUser", new { id = user.Id })!, "delete_user", "DELETE"));
            }

            if (requester.IsAdmin)
            {
                links.Add(new(Url.Link("ChangeUserRole", new { id = user.Id })!, "change_user_role", "PUT"));
            }

            return UserResponse.FromEntity(user, links);
        }
    }
}