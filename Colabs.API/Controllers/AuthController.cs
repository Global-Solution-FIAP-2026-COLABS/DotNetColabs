using System.IdentityModel.Tokens.Jwt;
using Colabs.Common;
using Colabs.Dtos;
using Colabs.Infrastructure;
using Colabs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colabs.Controllers
{
    /// <summary>
    /// Cadastro público, autenticação (login com e-mail e senha) e emissão de tokens JWT.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Cadastra um novo usuário (rota pública).
        /// </summary>
        /// <remarks>
        /// Todo cadastro cria um usuário com o papel <c>USER</c>. CPF aceita pontuação e é armazenado só com dígitos;
        /// o e-mail é armazenado em minúsculas. Latitude e longitude são opcionais, mas devem vir juntas.
        /// A senha é armazenada somente como hash e nunca é retornada.
        /// </remarks>
        /// <param name="request">Dados do usuário.</param>
        /// <response code="201">Usuário cadastrado.</response>
        /// <response code="400">Dados inválidos (campo ausente, CPF ou e-mail inválido etc.).</response>
        /// <response code="409">Já existe usuário com este CPF ou e-mail.</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);

            // O cabeçalho Location (GET /api/User/{id}) será adicionado no passo 5, quando o UserController existir.
            return result.IsSuccess
                ? StatusCode(StatusCodes.Status201Created, UserResponse.FromEntity(result.Value!))
                : this.ToErrorResult(result);
        }

        /// <summary>
        /// Autentica um usuário e devolve um token JWT.
        /// </summary>
        /// <remarks>
        /// Use o token no cabeçalho <c>Authorization: Bearer {token}</c> nas demais rotas.
        /// No Swagger, clique em <b>Authorize</b> e cole apenas o token.
        /// O token carrega o papel do usuário: se o papel mudar, é preciso fazer login de novo.
        /// </remarks>
        /// <param name="request">E-mail e senha.</param>
        /// <response code="200">Autenticado. Retorna o token, o tipo e a data de expiração (UTC).</response>
        /// <response code="400">Dados inválidos (e-mail mal formatado ou campos ausentes).</response>
        /// <response code="401">E-mail ou senha incorretos.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
        }

        /// <summary>
        /// Retorna os dados do usuário autenticado, lidos do token.
        /// </summary>
        /// <response code="200">Token válido.</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
        public IActionResult Me()
        {
            return Ok(new MeResponse(
                User.GetUserId(),
                User.FindFirst(JwtRegisteredClaimNames.Name)?.Value,
                User.FindFirst(JwtRegisteredClaimNames.Email)?.Value,
                User.FindFirst(AuthenticationExtensions.RoleClaimType)?.Value));
        }
    }
}