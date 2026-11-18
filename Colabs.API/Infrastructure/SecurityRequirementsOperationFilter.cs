using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Colabs.Infrastructure;

/// <summary>
/// Mostra o cadeado (Bearer) e as respostas 401 e 403 no Swagger somente nos endpoints que exigem autenticação.
/// As rotas públicas ([AllowAnonymous]: login e cadastro) ficam sem cadeado.
/// </summary>
public class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        var isAnonymous = metadata.OfType<IAllowAnonymous>().Any();
        var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any();

        if (isAnonymous || !requiresAuthorization)
            return;

        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Não autenticado: token JWT ausente, inválido ou expirado."
        });

        operation.Responses.TryAdd("403", new OpenApiResponse
        {
            Description = "Acesso negado: o usuário autenticado não tem permissão para este recurso."
        });

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                }] = Array.Empty<string>()
            }
        };
    }
}