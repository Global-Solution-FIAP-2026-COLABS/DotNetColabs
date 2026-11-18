using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Colabs.Common;
using Colabs.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Colabs.Infrastructure;

public static class AuthenticationExtensions
{
    /// <summary>Nome da claim que carrega o papel do usuário no token.</summary>
    public const string RoleClaimType = "role";

    /// <summary>
    /// Registra a autenticação JWT Bearer e as policies de autorização. A configuração é lida de forma tardia
    /// (IOptions) e validada na inicialização: se a seção "Jwt" estiver ausente ou inválida, a API falha ao subir
    /// com uma mensagem clara, em vez de devolver 500 em tempo de requisição.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .Validate(s => !string.IsNullOrWhiteSpace(s.Key) && Encoding.UTF8.GetByteCount(s.Key) >= 32,
                "Configuração 'Jwt:Key' ausente ou curta demais (mínimo de 32 caracteres).")
            .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer) && !string.IsNullOrWhiteSpace(s.Audience),
                "Configuração 'Jwt:Issuer' e 'Jwt:Audience' são obrigatórias.")
            .Validate(s => s.ExpirationMinutes is > 0 and <= 1440,
                "Configuração 'Jwt:ExpirationMinutes' deve estar entre 1 e 1440.")
            .ValidateOnStart();

        services.AddSingleton<ITokenService, TokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Mantém os nomes originais das claims ("sub", "email", "name", "role") em vez de remapeá-los.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),

                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,

                    NameClaimType = JwtRegisteredClaimNames.Name,
                    RoleClaimType = RoleClaimType
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.AdminOnly, policy => policy.RequireRole(AuthRoles.Admin));
        });

        return services;
    }
}