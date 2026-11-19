# 🤝 Colabs API

API RESTful da **Colabs**, uma plataforma comunitária que conecta necessidades reais a pessoas, instituições e organizações que podem ajudar. Desenvolvida em **ASP.NET Core 8** com **Oracle** (via Entity Framework Core e **Migrations**), a API é protegida por **JWT** com controle de acesso por papéis (`USER` e `ADMIN`), oferece **paginação, ordenação, filtros e HATEOAS** nas consultas, respostas de erro padronizadas (**ProblemDetails**, RFC 7807) e conta com observabilidade (logging estruturado, tracing e métricas).

---

## Equipe

| Nome | RM |
|------|----|
| _Anthony De Souza Henriques_ | _RM566188_ |
| _Guilherme Santos Fonseca_ | _RM564232_ |
| _Gustavo Araújo Da Silva_ | _RM566526_ |

---

## 📋 Sumário

- [Descrição do Projeto](#-descrição-do-projeto)
- [Tecnologias](#-tecnologias)
- [Instalação e Execução](#-instalação-e-execução)
  - [⚠️ Checklist antes de rodar](#️-checklist-antes-de-rodar)
  - [2. Configure as credenciais](#2-configure-as-credenciais-no-appsettingsjson-obrigatório)
  - [4. Execute as migrations](#4-execute-as-migrations-oracle)
  - [8. Crie o primeiro administrador](#8-crie-o-primeiro-administrador)
- [Autenticação e Autorização (JWT)](#-autenticação-e-autorização-jwt)
- [Paginação, Ordenação, Filtros e HATEOAS](#-paginação-ordenação-filtros-e-hateoas)
- [Padrão de Erros](#-padrão-de-erros)
- [Documentação das Rotas](#-documentação-das-rotas)
  - [Autenticação](#autenticação)
  - [Usuários](#usuários)
  - [Categorias](#categorias)
  - [Instituições](#instituições)
  - [Pedidos de Ajuda](#pedidos-de-ajuda)
  - [Doações](#doações)
  - [Avaliações](#avaliações)
  - [Histórico](#histórico)
- [Fluxo de uso da plataforma](#-fluxo-de-uso-da-plataforma)
- [Monitoramento e Observabilidade](#-monitoramento-e-observabilidade)
- [Testes Automatizados](#-testes-automatizados)
- [Estrutura do Banco de Dados](#-estrutura-do-banco-de-dados)
- [Status do desenvolvimento](#-status-do-desenvolvimento)
- [Solução de Problemas](#-solução-de-problemas)

---

## 📖 Descrição do Projeto

A **Colabs** reúne, em um só lugar, quem precisa de ajuda e quem pode ajudar. Pessoas cadastram **pedidos de ajuda** (com os itens de que precisam), outras pessoas e **instituições** cadastram **doações disponíveis**, e a plataforma registra o ciclo completo: do pedido à entrega, com **histórico de doações** e **avaliação das entregas**.

### Funcionalidades principais

- **Cadastro público de usuários** com CPF validado (dígitos verificadores), e-mail único e **senha armazenada como hash**
- **Login com JWT** (e-mail e senha) e **controle de acesso por papéis**: `USER` (comum) e `ADMIN`
- **Pedidos de ajuda** com itens, categoria, instituição opcional e **máquina de estados** (`Open`, `InProgress`, `Completed`, `Canceled`)
- **Doações disponíveis** com itens, doador pessoa ou instituição e **máquina de estados** (`Available`, `Matched`, `Delivered`, `Canceled`)
- **Instituições** (pontos de apoio) com CNPJ validado e coordenadas (latitude e longitude)
- **Categorias** para classificar pedidos e doações
- **Avaliação das entregas** (nota de 1 a 5) feita por quem recebeu a doação
- **Histórico de doações** do usuário, como doador e como recebedor
- **Paginação, ordenação e filtros** em todas as listagens, com **links HATEOAS** que respeitam a permissão de quem consulta
- **Erros padronizados** (ProblemDetails) com `correlationId`
- Documentação interativa via **Swagger UI** (com botão *Authorize* para o token)
- **Logging estruturado** (Serilog) com correlação de requisições
- **Tracing e métricas customizadas** via OpenTelemetry

### Arquitetura em camadas

```
Controllers ──► Services ──► AppDbContext (EF Core + Migrations) ──► Oracle
```

- **Controllers** — recebem a requisição HTTP, aplicam `[Authorize]` (e a policy `AdminOnly` quando necessário), traduzem o resultado do Service em resposta (200/201/204/400/401/403/404/409) e montam os **links HATEOAS**, sem regra de negócio.
- **Services** — concentram a regra de negócio (validações, dono ou admin, transições de status, filtros, ordenação e paginação). Devolvem sempre um `ServiceResult<T>`.
- **Models** — entidades do EF Core, mapeadas nas tabelas Oracle `DB_*`.
- **Dtos** — objetos de entrada e saída (`*Request`, `*Response` com `Links`, `*QueryParameters` com paginação e filtros).
- **Data** — `AppDbContext` (Fluent API: constraints, checks e conversão de enums) e conversores.
- **Common** — tipos reutilizáveis: `ServiceResult<T>`, `Requester`, `QueryParameters`, `PagedResult<T>`, `PagedResponse<T>`, `LinkDto`, `PaginationLinkBuilder`, `UserPasswordHasher`, validadores de CPF e CNPJ.
- **Infrastructure** — registro do Oracle, OpenTelemetry e autenticação JWT.
- **Middleware** — `CorrelationIdMiddleware` e `GlobalExceptionHandler`.
- **Diagnostics** — constantes de nomeação de tracing e métricas.

---

## 🛠 Tecnologias

**API**
- [.NET 8](https://dotnet.microsoft.com/) / [ASP.NET Core Web API](https://learn.microsoft.com/en-us/aspnet/core/) (Controllers)
- [Entity Framework Core 8](https://learn.microsoft.com/en-us/ef/core/) + [Oracle.EntityFrameworkCore 8.21.121](https://www.nuget.org/packages/Oracle.EntityFrameworkCore), com **Migrations**
- [Microsoft.AspNetCore.Authentication.JwtBearer 8](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer) — autenticação JWT
- ASP.NET Core Identity `PasswordHasher` — hash de senha (PBKDF2)
- [Swashbuckle (Swagger) 6.6.2](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)

**Observabilidade**
- [Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore) (Console + File)
- [OpenTelemetry](https://opentelemetry.io/docs/languages/net/) (tracing e métricas)
- Health Check de liveness (`/health/live`)

**Testes**
- [xUnit](https://xunit.net/), [Moq](https://github.com/devlooped/moq)
- [EF Core InMemory](https://learn.microsoft.com/en-us/ef/core/providers/in-memory/)

---

## 🚀 Instalação e Execução

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (o **runtime 8** é obrigatório — ter apenas o .NET 9/10 não basta)
- Acesso a uma instância **Oracle Database** (compatível com 19c) e suas credenciais, com permissão para criar tabelas
- A ferramenta `dotnet-ef` (instalada no [passo 4](#4-execute-as-migrations-oracle))
- [Oracle Client](https://www.oracle.com/database/technologies/instant-client.html) configurado (se necessário)

### ⚠️ Checklist antes de rodar

> [!IMPORTANT]
> **Quem for rodar o projeto precisa, obrigatoriamente, de DUAS coisas além do código:**
>
> 1. 🔑 **Criar o `appsettings.json` com as credenciais** (Oracle e chave JWT) — [ver passo 2](#2-configure-as-credenciais-no-appsettingsjson-obrigatório)
> 2. 🗄️ **Executar as migrations** para criar as tabelas no Oracle — [ver passo 4](#4-execute-as-migrations-oracle)
>
> O `appsettings.json` **não vem no repositório** (está no `.gitignore` por segurança). Sem ele, a API não sobe. Sem as migrations, as rotas de negócio falham por falta de tabelas.

### 1. Clone o repositório

```bash
git clone <URL-DO-REPOSITORIO>
cd Colabs
```

### 2. Configure as credenciais no `appsettings.json` (obrigatório)

> [!IMPORTANT]
> ### 🔑 Credenciais — o `appsettings.json` NÃO está no repositório
>
> Crie o arquivo **`Colabs.API/appsettings.json`** a partir do modelo `appsettings.json.example` e **preencha as credenciais**:
>
> ```bash
> # Linux / macOS / Git Bash
> cp Colabs.API/appsettings.json.example Colabs.API/appsettings.json
> ```
> ```powershell
> # Windows (PowerShell)
> Copy-Item Colabs.API\appsettings.json.example Colabs.API\appsettings.json
> ```
>
> Depois edite o `Colabs.API/appsettings.json`:
>
> ```json
> {
>   "Logging": {
>     "LogLevel": {
>       "Default": "Information",
>       "Microsoft.AspNetCore": "Warning"
>     }
>   },
>   "AllowedHosts": "*",
>   "ConnectionStrings": {
>     "OracleConnection": "User Id=SEU_USUARIO;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/orcl;"
>   },
>   "Jwt": {
>     "Key": "TROQUE-POR-UMA-CHAVE-SECRETA-COM-NO-MINIMO-32-CARACTERES",
>     "Issuer": "Colabs.API",
>     "Audience": "Colabs.Clients",
>     "ExpirationMinutes": 120
>   }
> }
> ```

| Seção | Campo | O que colocar |
|---|---|---|
| `ConnectionStrings` | `OracleConnection` | **Seu usuário e senha do Oracle** (`User Id`, `Password`) e o `Data Source` do seu banco. A senha diferencia maiúsculas de minúsculas. |
| `Jwt` | `Key` | **Chave secreta própria**, com **no mínimo 32 caracteres** (veja abaixo como gerar) |
| `Jwt` | `Issuer` / `Audience` | Podem ficar como no exemplo |
| `Jwt` | `ExpirationMinutes` | Validade do token em minutos (1 a 1440; padrão 120) |

**Gerando uma chave JWT:**

```bash
openssl rand -base64 48                                   # Linux / macOS / Git Bash
```
```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))   # PowerShell
```

> [!WARNING]
> - **Nunca faça commit** do `appsettings.json` nem compartilhe sua senha do Oracle ou a chave JWT (o arquivo já está no `.gitignore`).
> - Se `Jwt` estiver ausente ou com chave curta, a API **falha ao iniciar** com uma mensagem clara no log (`Configuração 'Jwt:Key' ausente ou curta demais...`).
> - Alternativa mais segura em desenvolvimento: **user-secrets**, que guardam as credenciais fora da pasta do projeto:
>   ```bash
>   dotnet user-secrets init --project Colabs.API
>   dotnet user-secrets set "ConnectionStrings:OracleConnection" "User Id=SEU_USUARIO;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/orcl;" --project Colabs.API
>   dotnet user-secrets set "Jwt:Key" "SUA-CHAVE-COM-32-OU-MAIS-CARACTERES" --project Colabs.API
>   ```
>   Em produção, use variáveis de ambiente (ex.: `Jwt__Key`, `ConnectionStrings__OracleConnection`).

### 3. Restaure as dependências

```bash
dotnet restore
```

### 4. Execute as migrations (Oracle)

Cria todas as tabelas (`DB_*`) no Oracle, com as constraints, os checks e as colunas de autenticação (`PASSWORD` e `ROLE`) de `DB_USER`:

```bash
# apenas na primeira vez, se ainda não tiver a ferramenta do EF Core:
dotnet tool install --global dotnet-ef --version 8.0.11

dotnet ef database update --project Colabs.API
```

> [!NOTE]
> O schema é criado **somente pelas migrations** (pasta `Colabs.API/Migrations`). Se as tabelas `DB_*` já existirem no seu usuário Oracle (por exemplo, de uma execução manual de script SQL), a migration falha com `ORA-00955`. Nesse caso, apague as tabelas antigas antes (`DROP TABLE ... CASCADE CONSTRAINTS`) e rode o comando de novo.

### 5. Execute a aplicação

```bash
dotnet run --project Colabs.API --launch-profile https
```

A API estará disponível em:
- HTTP: `http://localhost:5257`
- HTTPS: `https://localhost:7224`

> Na primeira vez, confie no certificado de desenvolvimento: `dotnet dev-certs https --trust`.

### 6. Acesse o Swagger

Em modo desenvolvimento:

```
https://localhost:7224/swagger
```

### 7. Verifique se está de pé

Abra `https://localhost:7224/health/live`. A resposta deve ser `Healthy`.

### 8. Crie o primeiro administrador

Todo cadastro público cria um usuário `USER`. O primeiro `ADMIN` é promovido diretamente no banco (a partir daí, administradores promovem outros usuários pela própria API, em `PUT /api/User/{id}/role`):

1. Cadastre-se em `POST /api/Auth/register`.
2. No Oracle, promova o usuário:

   ```sql
   UPDATE DB_USER SET ROLE = 'ADMIN' WHERE EMAIL = 'seu@email.com';
   COMMIT;
   ```
3. Faça **login de novo** em `POST /api/Auth/login`: o papel vai dentro do token, então o token antigo continua `USER`.

---

## 🔐 Autenticação e Autorização (JWT)

Todas as rotas exigem um **token JWT**, exceto as públicas:

| Rota pública | Para quê |
|---|---|
| `POST /api/Auth/register` | Cadastro de usuário (ponto de partida) |
| `POST /api/Auth/login` | Login e emissão do token |
| `GET /health/live` | Monitoramento |

O login é feito com o **e-mail e a senha do usuário**. A senha é armazenada **somente como hash** (ASP.NET Core Identity `PasswordHasher`, PBKDF2 com sal) e nunca é retornada pela API.

### Papéis e permissões

| Papel | O que pode |
|---|---|
| `USER` | Consultar tudo (categorias, instituições, pedidos, doações, avaliações); criar os próprios pedidos e doações; gerenciar **os próprios** cadastro, pedidos e doações; avaliar as doações que recebeu |
| `ADMIN` | Tudo o que o `USER` faz, **em qualquer recurso**, mais: gerenciar categorias e instituições, listar usuários e alterar papéis |

O **dono** de um recurso é: o próprio usuário (cadastro), o solicitante (pedido de ajuda), o doador (doação) e, para avaliações, quem recebeu a doação (dono do pedido vinculado).

### Fluxo (Swagger)

1. `POST /api/Auth/register` — cadastre um usuário (e-mail e CPF **novos** e válidos).
2. `POST /api/Auth/login` — informe `email` e `password`; copie o valor de `token` da resposta.
3. Clique em **Authorize** (🔓 no topo do Swagger), cole **apenas o token** (o prefixo `Bearer` é automático) e confirme.
4. Chame qualquer rota protegida. Sem token (ou com token inválido ou expirado) a resposta é **`401 Unauthorized`**; sem permissão para o recurso, **`403 Forbidden`**.

### Fluxo (linha de comando)

```bash
# 1) Cadastrar o usuário (rota pública)
curl -X POST https://localhost:7224/api/Auth/register -k \
  -H "Content-Type: application/json" \
  -d '{"name":"Maria Silva","cpf":"529.982.247-25","phone":"(11) 99999-0000","email":"maria@email.com","password":"SenhaForte123","address":"Rua das Flores, 100 - São Paulo"}'

# 2) Login -> guarda o token
TOKEN=$(curl -sk -X POST https://localhost:7224/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"maria@email.com","password":"SenhaForte123"}' | jq -r .token)

# 3) Usar o token
curl -k "https://localhost:7224/api/Category?pageSize=5" -H "Authorization: Bearer $TOKEN"
```

```powershell
# PowerShell
$login = Invoke-RestMethod -Method Post -Uri https://localhost:7224/api/Auth/login `
  -ContentType "application/json" -Body '{"email":"maria@email.com","password":"SenhaForte123"}'

Invoke-RestMethod -Uri "https://localhost:7224/api/Category?pageSize=5" `
  -Headers @{ Authorization = "Bearer $($login.token)" }
```

**Resposta do login (`200 OK`):**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresAt": "2026-11-19T20:30:00Z",
  "userId": 1,
  "name": "Maria Silva",
  "email": "maria@email.com",
  "role": "USER"
}
```

O token (HS256) carrega as claims `sub` (id do usuário), `email`, `name`, `role` e `jti`, e expira em `Jwt:ExpirationMinutes` (padrão 120 min).

> ℹ️ O login não diferencia "e-mail inexistente" de "senha incorreta" (ambos `401`, com a mesma mensagem e tempo de resposta equivalente), para não revelar quais e-mails existem.
>
> ℹ️ O JWT não tem estado: **mudanças de papel valem só a partir de um novo login**, e o token de um usuário removido continua válido até expirar.

---

## 📑 Paginação, Ordenação, Filtros e HATEOAS

Todas as **listagens** aceitam os mesmos parâmetros de query, mais filtros específicos de cada recurso (listados na [documentação das rotas](#-documentação-das-rotas)).

| Parâmetro | Padrão | Descrição |
|---|---|---|
| `pageNumber` | `1` | Página desejada (valores menores que 1 viram 1) |
| `pageSize` | `10` | Itens por página (**máximo 50**; acima disso é ajustado para 50) |
| `sortBy` | depende do recurso | Campo de ordenação (campo desconhecido usa o padrão do recurso) |
| `ascending` | `true` | `true` crescente, `false` decrescente |

> 💡 Nas listagens de **pedidos, doações, avaliações e histórico**, a ordenação padrão é por **data, crescente**. Para ver os mais recentes primeiro, use `ascending=false`.

A ordenação sempre desempata pelo `Id`, então a paginação é **estável** (nenhum item se repete ou some entre páginas).

### Formato da resposta (`PagedResponse`)

Exemplo: `GET /api/HelpRequest?status=Open&pageSize=10&ascending=false`

```json
{
  "items": [
    {
      "id": 1,
      "idCategory": 1,
      "categoryName": "Alimentos",
      "idUser": 2,
      "idInstitution": null,
      "description": "Preciso de cestas básicas para minha família",
      "status": "Open",
      "creationDate": "2026-11-19T15:10:00",
      "completionDate": null,
      "items": [
        { "id": 1, "name": "Cesta básica", "quantity": 2 },
        { "id": 2, "name": "Leite", "quantity": 6 }
      ],
      "links": [
        { "href": "https://localhost:7224/api/HelpRequest/1", "rel": "self", "method": "GET" },
        { "href": "https://localhost:7224/api/HelpRequest/1", "rel": "update_help_request", "method": "PUT" },
        { "href": "https://localhost:7224/api/HelpRequest/1/status", "rel": "change_help_request_status", "method": "PUT" },
        { "href": "https://localhost:7224/api/HelpRequest/1", "rel": "delete_help_request", "method": "DELETE" }
      ]
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1,
  "totalRecords": 1,
  "links": [
    {
      "href": "https://localhost:7224/api/HelpRequest?status=Open&pageNumber=1&pageSize=10&ascending=False",
      "rel": "self",
      "method": "GET"
    }
  ]
}
```

### Links HATEOAS

- **Links da página** (`links` na raiz): `self`, `first_page`, `prev_page`, `next_page` e `last_page` (só aparecem quando existem). Eles **preservam os filtros, a ordenação e o tamanho da página**, então basta seguir o `href`.
- **Links de cada item** (`links` dentro do item): ações do recurso (`self`, `update_*`, `delete_*`, `change_*_status`). **Só aparecem as ações que o usuário autenticado pode executar** (por exemplo, `update_help_request` aparece apenas para o dono do pedido e para administradores).
- `GET` por id, `POST` e as mudanças de status também devolvem o recurso com seus `links`.

---

## ❗ Padrão de Erros

Todas as respostas de erro seguem o padrão **ProblemDetails** (RFC 7807, `application/problem+json`) e trazem o `correlationId` da requisição, o mesmo valor do header `X-Correlation-Id` e das linhas de log:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflito com o estado atual do recurso.",
  "status": 409,
  "detail": "Já existe um usuário cadastrado com este CPF.",
  "traceId": "00-5f3c...-01",
  "correlationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

| Código | Quando acontece |
|---|---|
| `400 Bad Request` | Dados inválidos. Erros de validação trazem a lista por campo (`errors`); regras de negócio trazem o motivo em `detail` (ex.: categoria inexistente) |
| `401 Unauthorized` | Token ausente, inválido ou expirado; e-mail ou senha incorretos no login |
| `403 Forbidden` | Usuário autenticado, mas sem permissão para o recurso (não é o dono, não é admin) |
| `404 Not Found` | Recurso não encontrado |
| `409 Conflict` | Conflito com o estado atual: duplicidade (CPF, e-mail, CNPJ, nome de categoria, avaliação), transição de status não permitida, exclusão de registro em uso |
| `500 Internal Server Error` | Erro inesperado (detalhes internos nunca vão na resposta; procure o `correlationId` no log) |

Exemplo de erro de validação (`400`):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Cpf": ["CPF inválido."],
    "Password": ["A senha deve ter entre 8 e 100 caracteres."]
  },
  "traceId": "00-5f3c...-01",
  "correlationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

## 📡 Documentação das Rotas

> Legenda de acesso: 🔓 rota pública · 🔒 exige token JWT · 👤 somente o dono ou um administrador · 👑 somente administradores.
> Os corpos JSON usam `camelCase`. Valores de enum (status, papel) são aceitos em texto: `Open`, `InProgress`, `Completed`, `Canceled`, `Available`, `Matched`, `Delivered`, `User`, `Admin`. Datas em filtros aceitam o formato `yyyy-MM-dd`.

### Autenticação

Base URL: `/api/Auth`

| Método | Rota | Acesso | Descrição | Corpo | Respostas |
|--------|------|:---:|-----------|-------|-----------|
| `POST` | `/api/Auth/register` | 🔓 | Cadastra um usuário (sempre com o papel `USER`) | `RegisterRequest` | `201 Created` (com `Location`) / `400` / `409` |
| `POST` | `/api/Auth/login` | 🔓 | Autentica e devolve o token | `LoginRequest` | `200 OK` / `400` / `401` |
| `GET` | `/api/Auth/me` | 🔒 | Dados do usuário autenticado (lidos do token) | — | `200 OK` / `401` |

**RegisterRequest:**
```json
{
  "name": "Maria Silva",
  "cpf": "529.982.247-25",
  "phone": "(11) 99999-0000",
  "email": "maria@email.com",
  "password": "SenhaForte123",
  "address": "Rua das Flores, 100 - São Paulo",
  "latitude": -23.550520,
  "longitude": -46.633308
}
```

**LoginRequest:**
```json
{ "email": "maria@email.com", "password": "SenhaForte123" }
```

> ⚠️ O CPF é validado pelos dígitos verificadores e guardado só com números; o e-mail é guardado em minúsculas. CPF e e-mail devem ser únicos. `latitude` e `longitude` são opcionais, mas devem ser informadas **juntas**. A senha tem de 8 a 100 caracteres. O papel nunca é aceito no cadastro.

---

### Usuários

Base URL: `/api/User`. O cadastro público é feito em `POST /api/Auth/register`.

| Método | Rota | Acesso | Descrição | Corpo | Respostas |
|--------|------|:---:|-----------|-------|-----------|
| `GET` | `/api/User` | 👑 | Lista os usuários (paginado, sem expor senha) | — | `200 OK` / `403` |
| `GET` | `/api/User/{id}` | 👤 | Busca um usuário pelo ID | — | `200 OK` / `403` / `404` |
| `PUT` | `/api/User/{id}` | 👤 | Atualiza os dados cadastrais | `UserUpdateRequest` | `204 No Content` / `400` / `403` / `404` / `409` |
| `DELETE` | `/api/User/{id}` | 👤 | Remove um usuário | — | `204 No Content` / `403` / `404` / `409` |
| `PUT` | `/api/User/{id}/role` | 👑 | Promove ou rebaixa o papel de um usuário | `UserRoleRequest` | `200 OK` / `400` / `403` / `404` / `409` |

**Filtros e ordenação de `GET /api/User`:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `name` | texto | Nome contém o texto (ignora maiúsculas) |
| `email` | texto | E-mail contém o texto (ignora maiúsculas) |
| `cpf` | texto | CPF contém o texto (com ou sem pontuação) |
| `role` | `User` / `Admin` | Papel exato |
| `sortBy` | `name` (padrão), `email`, `id` | Campo de ordenação |

Exemplo: `/api/User?role=Admin&sortBy=email&pageSize=5`

**UserUpdateRequest** (CPF, senha e papel não podem ser alterados aqui):
```json
{
  "name": "Maria Souza",
  "phone": "(11) 98888-7777",
  "email": "maria.souza@email.com",
  "address": "Av. Paulista, 1000 - São Paulo",
  "latitude": -23.561414,
  "longitude": -46.655881
}
```

**UserRoleRequest:**
```json
{ "role": "Admin" }
```

> ⚠️ Um usuário comum só enxerga e altera o **próprio** cadastro (pedir o de outra pessoa retorna `403`, mesmo que o Id não exista). Um administrador **não pode rebaixar a si mesmo**, e **o último administrador não pode ser removido ou rebaixado**. Usuários com pedidos de ajuda ou doações vinculados não podem ser removidos (`409`).

---

### Categorias

Base URL: `/api/Category` — leitura para qualquer usuário autenticado; escrita somente para administradores.

| Método | Rota | Acesso | Descrição | Corpo | Respostas |
|--------|------|:---:|-----------|-------|-----------|
| `GET` | `/api/Category` | 🔒 | Lista as categorias (paginado) | — | `200 OK` |
| `GET` | `/api/Category/{id}` | 🔒 | Busca uma categoria pelo ID | — | `200 OK` / `404` |
| `POST` | `/api/Category` | 👑 | Cadastra uma categoria | `CategoryRequest` | `201 Created` / `400` / `403` / `409` |
| `PUT` | `/api/Category/{id}` | 👑 | Atualiza uma categoria | `CategoryRequest` | `204 No Content` / `400` / `403` / `404` / `409` |
| `DELETE` | `/api/Category/{id}` | 👑 | Remove uma categoria | — | `204 No Content` / `403` / `404` / `409` |

**Filtros e ordenação de `GET /api/Category`:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `name` | texto | Nome contém o texto |
| `sortBy` | `name` (padrão), `id` | Campo de ordenação |

**CategoryRequest:**
```json
{ "name": "Alimentos" }
```

> ⚠️ O nome é único (sem diferenciar maiúsculas). Categorias em uso por pedidos ou doações não podem ser removidas (`409`).

---

### Instituições

Base URL: `/api/Institution` — instituições e organizações que atuam como **pontos de apoio**. Leitura para qualquer usuário autenticado; escrita somente para administradores.

| Método | Rota | Acesso | Descrição | Corpo | Respostas |
|--------|------|:---:|-----------|-------|-----------|
| `GET` | `/api/Institution` | 🔒 | Lista as instituições (paginado) | — | `200 OK` |
| `GET` | `/api/Institution/{id}` | 🔒 | Busca uma instituição pelo ID | — | `200 OK` / `404` |
| `POST` | `/api/Institution` | 👑 | Cadastra uma instituição | `InstitutionRequest` | `201 Created` / `400` / `403` / `409` |
| `PUT` | `/api/Institution/{id}` | 👑 | Atualiza uma instituição (o CNPJ não muda) | `InstitutionUpdateRequest` | `204 No Content` / `400` / `403` / `404` |
| `DELETE` | `/api/Institution/{id}` | 👑 | Remove uma instituição | — | `204 No Content` / `403` / `404` / `409` |

**Filtros e ordenação de `GET /api/Institution`:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `name` | texto | Nome contém o texto |
| `cnpj` | texto | CNPJ contém o texto (com ou sem pontuação) |
| `address` | texto | Endereço contém o texto |
| `sortBy` | `name` (padrão), `cnpj`, `address` | Campo de ordenação |

**InstitutionRequest:**
```json
{
  "name": "Casa Esperança",
  "cnpj": "11.222.333/0001-81",
  "address": "Rua das Flores, 10 - São Paulo",
  "latitude": -23.550520,
  "longitude": -46.633308
}
```

**InstitutionUpdateRequest** (sem CNPJ):
```json
{
  "name": "Casa Nova Esperança",
  "address": "Av. Paulista, 1000 - São Paulo",
  "latitude": -23.561414,
  "longitude": -46.655881
}
```

> ⚠️ O CNPJ é validado pelos dígitos verificadores, guardado só com números e deve ser único. Latitude (-90 a 90) e longitude (-180 a 180) são **obrigatórias**. Instituições com pedidos ou doações vinculados não podem ser removidas (`409`).

---

### Pedidos de Ajuda

Base URL: `/api/HelpRequest` — qualquer usuário autenticado consulta e cria; editar, mudar o status e excluir é permitido ao **dono do pedido** ou a um administrador.

| Método | Rota | Acesso | Descrição | Corpo | Respostas |
|--------|------|:---:|-----------|-------|-----------|
| `GET` | `/api/HelpRequest` | 🔒 | Lista os pedidos (paginado) | — | `200 OK` |
| `GET` | `/api/HelpRequest/{id}` | 🔒 | Busca um pedido pelo ID | — | `200 OK` / `404` |
| `POST` | `/api/HelpRequest` | 🔒 | Cria um pedido (status `Open`) em nome do usuário autenticado | `HelpRequestRequest` | `201 Created` / `400` |
| `PUT` | `/api/HelpRequest/{id}` | 👤 | Atualiza o pedido (só `Open`; substitui os itens) | `HelpRequestRequest` | `204 No Content` / `400` / `403` / `404` / `409` |
| `PUT` | `/api/HelpRequest/{id}/status` | 👤 | Muda o status do pedido | `HelpRequestStatusRequest` | `200 OK` / `400` / `403` / `404` / `409` |
| `DELETE` | `/api/HelpRequest/{id}` | 👤 | Remove um pedido | — | `204 No Content` / `403` / `404` / `409` |

**Filtros e ordenação de `GET /api/HelpRequest`:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `status` | enum | `Open`, `InProgress`, `Completed` ou `Canceled` |
| `idCategory` | inteiro | Pedidos de uma categoria |
| `idUser` | inteiro | Pedidos de um usuário (use o seu id para listar só os seus) |
| `description` | texto | Descrição contém o texto |
| `sortBy` | `creationDate` (padrão), `status`, `id` | Campo de ordenação |

Exemplo: `/api/HelpRequest?status=Open&idCategory=1&ascending=false`

**HelpRequestRequest:**
```json
{
  "idCategory": 1,
  "idInstitution": null,
  "description": "Preciso de cestas básicas para minha família",
  "items": [
    { "name": "Cesta básica", "quantity": 2 },
    { "name": "Leite", "quantity": 6 }
  ]
}
```

**HelpRequestStatusRequest:**
```json
{ "status": "InProgress" }
```

**Transições de status permitidas:**

| De | Para |
|---|---|
| `Open` | `InProgress`, `Canceled` |
| `InProgress` | `Completed`, `Canceled` |
| `Completed` | — (final) |
| `Canceled` | — (final) |

> ⚠️ O dono do pedido é sempre o usuário do token (nunca vem no corpo). A categoria (e a instituição, se informada) deve existir, e o pedido precisa de **ao menos 1 item**, com quantidade de 1 a 99.999.999. `Completed` e `Canceled` registram `completionDate`. A edição só vale com o pedido `Open`, e a lista de itens enviada **substitui** a atual. Pedidos com doações vinculadas não podem ser removidos (`409`): cancele-os.

---

### Doações

Base URL: `/api/Donation` — qualquer usuário autenticado consulta e cadastra; editar, mudar o status e excluir é permitido ao **doador** ou a um administrador.

| Método | Rota | Acesso | Descrição | Corpo | Respostas |
|--------|------|:---:|-----------|-------|-----------|
| `GET` | `/api/Donation` | 🔒 | Lista as doações (paginado) | — | `200 OK` |
| `GET` | `/api/Donation/{id}` | 🔒 | Busca uma doação pelo ID | — | `200 OK` / `404` |
| `POST` | `/api/Donation` | 🔒 | Cadastra uma doação (status `Available`) em nome do usuário autenticado | `DonationRequest` | `201 Created` / `400` |
| `PUT` | `/api/Donation/{id}` | 👤 | Atualiza a doação (só `Available`; substitui os itens) | `DonationRequest` | `204 No Content` / `400` / `403` / `404` / `409` |
| `PUT` | `/api/Donation/{id}/status` | 👤 | Muda o status da doação | `DonationStatusRequest` | `200 OK` / `400` / `403` / `404` / `409` |
| `DELETE` | `/api/Donation/{id}` | 👤 | Remove uma doação | — | `204 No Content` / `403` / `404` / `409` |

**Filtros e ordenação de `GET /api/Donation`:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `status` | enum | `Available`, `Matched`, `Delivered` ou `Canceled` |
| `idCategory` | inteiro | Doações de uma categoria |
| `idUser` | inteiro | Doações de um doador |
| `idHelpRequest` | inteiro | Doações vinculadas a um pedido |
| `description` | texto | Descrição contém o texto |
| `sortBy` | `creationDate` (padrão), `status`, `id` | Campo de ordenação |

Exemplo: `/api/Donation?status=Available&idCategory=1&ascending=false`

**DonationRequest:**
```json
{
  "idCategory": 1,
  "idInstitution": null,
  "description": "Cestas básicas e leite para doar",
  "items": [
    { "name": "Cesta básica", "quantity": 3 },
    { "name": "Leite", "quantity": 12 }
  ]
}
```

**DonationStatusRequest:**
```json
{ "status": "Delivered" }
```

**Transições de status permitidas:**

| De | Para | Observação |
|---|---|---|
| `Available` | `Matched` | Acontece apenas ao efetivar a correspondência com um pedido |
| `Available` | `Canceled` | Doador ou administrador |
| `Matched` | `Delivered` | Doador, administrador ou **dono do pedido vinculado** (confirma o recebimento) |
| `Matched` | `Canceled` | Doador ou administrador |
| `Delivered` / `Canceled` | — | Estados finais |

> ⚠️ O doador é sempre o usuário do token; `idInstitution` (opcional) indica que a doação é feita em nome de uma instituição. `Delivered` **conclui o pedido** vinculado (`InProgress` para `Completed`); `Canceled` **devolve o pedido para `Open`** quando não resta outra doação ativa nele. A doação cancelada mantém o vínculo, para preservar o histórico. Doações `Matched` ou `Delivered` não podem ser removidas (`409`).

---

### Avaliações

Base URL: `/api/Review` — avaliação das entregas. Leitura para qualquer usuário autenticado; criar, editar e excluir é permitido a **quem recebeu a doação** (dono do pedido vinculado) ou a um administrador.

| Método | Rota | Acesso | Descrição | Corpo | Respostas |
|--------|------|:---:|-----------|-------|-----------|
| `GET` | `/api/Review` | 🔒 | Lista as avaliações (paginado) | — | `200 OK` |
| `GET` | `/api/Review/{id}` | 🔒 | Busca uma avaliação pelo ID | — | `200 OK` / `404` |
| `POST` | `/api/Review` | 👤 | Avalia uma doação entregue | `ReviewRequest` | `201 Created` / `400` / `403` / `404` / `409` |
| `PUT` | `/api/Review/{id}` | 👤 | Atualiza a nota e o comentário | `ReviewUpdateRequest` | `204 No Content` / `400` / `403` / `404` |
| `DELETE` | `/api/Review/{id}` | 👤 | Remove uma avaliação | — | `204 No Content` / `403` / `404` |

**Filtros e ordenação de `GET /api/Review`:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `idDonation` | inteiro | Avaliação de uma doação |
| `rating` | inteiro (1 a 5) | Nota exata |
| `sortBy` | `reviewDate` (padrão), `rating`, `id` | Campo de ordenação |

**ReviewRequest:**
```json
{
  "idDonation": 1,
  "rating": 5,
  "description": "Entrega rápida e tudo certo!"
}
```

**ReviewUpdateRequest:**
```json
{
  "rating": 4,
  "description": "Chegou um dia depois do combinado."
}
```

> ⚠️ A nota vai de 1 a 5. Só doações **`Delivered`** podem ser avaliadas (`409` caso contrário), e **cada doação recebe uma única avaliação** (`409` se já avaliada). Quem avalia é quem recebeu a doação, isto é, o dono do pedido vinculado.

---

### Histórico

Base URL: `/api/History`

| Método | Rota | Acesso | Descrição | Respostas |
|--------|------|:---:|-----------|-----------|
| `GET` | `/api/History/donations` | 🔒 | Histórico de doações do usuário autenticado, como doador e como recebedor (paginado) | `200 OK` / `403` |

**Filtros e ordenação de `GET /api/History/donations`:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `role` | `Donor` / `Recipient` | Somente como doador ou somente como recebedor. Sem valor: as duas |
| `status` | enum | Status da doação: `Available`, `Matched`, `Delivered` ou `Canceled` |
| `from` / `to` | data `yyyy-MM-dd` | Período de criação (inclusive; `to` inclui o dia inteiro) |
| `idUser` | inteiro | **Somente administradores:** histórico de outro usuário. Usuário comum que pedir o de outra pessoa recebe `403` |
| `sortBy` | `creationDate` (padrão), `status`, `id` | Campo de ordenação |

Exemplo: `/api/History/donations?role=Donor&status=Delivered&from=2026-01-01&ascending=false`

Cada item traz o **papel do usuário** na doação (`Donor` ou `Recipient`), o status, a categoria, os itens e a **nota da avaliação** (`rating`), quando houver.

---

## 🔄 Fluxo de uso da plataforma

1. **Maria** e **João** se cadastram (`POST /api/Auth/register`) e fazem login.
2. Um administrador cria as **categorias** (`POST /api/Category`) e as **instituições** (`POST /api/Institution`).
3. **João** registra um **pedido de ajuda** (`POST /api/HelpRequest`), com os itens de que precisa. O pedido nasce `Open`.
4. **Maria** registra uma **doação** (`POST /api/Donation`), na mesma categoria. A doação nasce `Available`.
5. A doação é vinculada ao pedido: a doação passa a `Matched` e o pedido a `InProgress`.
6. Entregue a doação, ela é marcada como `Delivered` (pelo doador, por um administrador ou por João, que recebeu): o **pedido é concluído** automaticamente (`Completed`).
7. **João** avalia a entrega (`POST /api/Review`), com nota de 1 a 5.
8. Os dois consultam o **histórico** (`GET /api/History/donations`), com o papel de cada um em cada doação e a nota recebida.

Se a doação for cancelada antes da entrega, o pedido volta para `Open` e pode receber outra doação.

---

## 🩺 Monitoramento e Observabilidade

### Health Check

| Endpoint | Verifica | Acesso |
|---|---|---|
| `GET /health/live` | Se o processo da API está de pé (não depende de nada externo) | Público, sem token |

Responde `200 OK` com o texto `Healthy`.

### Logging Estruturado

O logging é feito com **Serilog**, configurado em `Program.cs`:

- **Console** — acompanhamento em tempo real durante o desenvolvimento.
- **Arquivo** — `logs/colabs-AAAAMMDD.log`, com rotação diária e retenção dos últimos 14 arquivos (pasta ignorada no `.gitignore`).
- **Níveis**: `Information` (operações de negócio bem-sucedidas), `Warning` (regras violadas — registro não encontrado, acesso negado, transição inválida, falha de login) e `Error` (exceções não tratadas, capturadas pelo `GlobalExceptionHandler`).
- **Correlação de requisições** — o `CorrelationIdMiddleware` gera (ou reaproveita) um `X-Correlation-Id`, devolve o valor no header de resposta, nas respostas de erro (`correlationId`) e em toda linha de log da requisição:

```bash
grep "3fa85f64-5717-4562-b3fc-2c963f66afa6" logs/colabs-20261119.log
```

- Toda requisição HTTP concluída gera uma linha de log automática (`UseSerilogRequestLogging`), com método, path, status e duração.
- 🔒 **Privacidade (LGPD):** os logs registram apenas **Ids**. Nome, e-mail, CPF e senha nunca são gravados.
- 💡 Ao investigar um erro, procure no arquivo de log a linha `[ERR]`: ela traz o *stack trace* completo (o console também imprime dados do OpenTelemetry, que podem esconder o erro).

### Tracing e Métricas (OpenTelemetry)

Configurado em `Infrastructure/DependencyInjection.cs`:

- **Tracing** — cada operação de negócio nos `Services` (`RegisterUser`, `Login`, `CreateHelpRequest`, `ChangeDonationStatus`...) gera um *span* nomeado, com tags relevantes (`user.id`, `helprequest.id`, `donation.id`, `page.number`, `page.size`...) e status de erro quando a regra falha. Somado a isso, `AddAspNetCoreInstrumentation()` gera spans automáticos para cada requisição HTTP.
- **Métricas de negócio** — contadores customizados: `users_registered_total`, `login_failures_total`, `categories_created_total`, `institutions_created_total`, `help_requests_created_total`, `donations_created_total` e `reviews_created_total`.
- **Tempo de resposta e taxa de erro** — cobertos pela instrumentação padrão do ASP.NET Core (`http.server.request.duration`).
- **Exportação** — por padrão, para o **Console** (`AddConsoleExporter()`). Em produção, troque por `AddOtlpExporter()` apontando para um coletor (Jaeger, Grafana Tempo, Application Insights...).

---

## 🧪 Testes Automatizados

Os testes seguem o padrão **AAA (Arrange, Act, Assert)** e a nomenclatura `MetodoTestado_Cenario_ResultadoEsperado`.

| Projeto | O que testa | Como isola dependências |
|---|---|---|
| `Colabs.UnitTests` | Regras de negócio dos `Services` de autenticação, categorias, instituições e usuários (paginação, filtros, dono ou admin, papéis); validadores de CPF e CNPJ; hash de senha; geração do JWT; contratos de paginação (`QueryParameters`, `PagedResult`, `PaginationLinkBuilder`) e `ServiceResult` | `ILogger` nulo e `IMeterFactory` mockado com **Moq**; `AppDbContext` com **EF Core InMemory** (um banco novo por teste) |
| `Colabs.IntegrationTests` | Projeto reservado para os testes de integração dos endpoints (ainda sem testes) | — |

> ✅ Os testes unitários **não precisam do Oracle**: rodam isolados e de forma determinística.

```bash
# Rodar todos os testes
dotnet test

# Apenas os unitários
dotnet test Colabs.UnitTests

# Filtrar por nome (classe ou teste)
dotnet test --filter "FullyQualifiedName~UserService"

# Saída detalhada
dotnet test --logger "console;verbosity=detailed"
```

> A solução usa o formato `.slnx`, que exige um .NET SDK recente (9.0.200 ou superior). Com o SDK 8, rode os projetos individualmente (`dotnet test Colabs.UnitTests`).

No **VS Code** com C# Dev Kit, os testes aparecem no **Test Explorer** (ícone de frasco) depois do build.

---

## 🗂 Estrutura do Banco de Dados

**Oracle (relacional — EF Core, criado pelas migrations)**

```
DB_USER (usuários; PASSWORD = hash, ROLE = USER | ADMIN)
  ├── DB_HELP_REQUEST (pedidos de ajuda, FK → DB_USER, DB_CATEGORY, DB_INSTITUTION opcional)
  │     ├── DB_HELP_REQUEST_ITEM (itens do pedido, FK → DB_HELP_REQUEST)
  │     └── DB_DONATION (doação vinculada ao pedido, FK opcional)
  └── DB_DONATION (doações, FK → DB_USER ou DB_INSTITUTION, DB_CATEGORY)
        ├── DB_DONATION_ITEM (itens da doação, FK → DB_DONATION)
        └── DB_REVIEW (avaliação da entrega, FK → DB_DONATION)

DB_INSTITUTION (instituições / pontos de apoio, com latitude e longitude)
DB_CATEGORY (categorias de pedidos e doações)
```

| Tabela | Regras garantidas no banco |
|---|---|
| `DB_USER` | `CPF` e `EMAIL` únicos; `ROLE` em (`USER`, `ADMIN`) |
| `DB_INSTITUTION` | `CNPJ` único; latitude e longitude obrigatórias |
| `DB_CATEGORY` | `NAME` único |
| `DB_HELP_REQUEST` | `STATUS` em (`OPEN`, `IN_PROGRESS`, `COMPLETED`, `CANCELED`) |
| `DB_HELP_REQUEST_ITEM` / `DB_DONATION_ITEM` | `QUANTITY` > 0 |
| `DB_DONATION` | `STATUS` em (`AVAILABLE`, `MATCHED`, `DELIVERED`, `CANCELED`); ao menos um doador (`ID_USER` ou `ID_INSTITUTION`) |
| `DB_REVIEW` | `RATING` entre 1 e 5 |

Os status e o papel são enums no C#, gravados no banco como texto em `UPPER_SNAKE_CASE`. Nenhuma FK usa `ON DELETE CASCADE`: pedidos e doações são **cancelados por status**, não apagados.

---

## 🚧 Status do desenvolvimento

Funcionalidades previstas que **ainda não fazem parte desta versão**:

- **Mapa de pontos de apoio e correspondência (match):** busca de instituições por proximidade e endpoint para vincular uma doação a um pedido. Hoje a transição `Available` para `Matched` só será possível quando esse endpoint existir.
- **Idempotência** nas requisições `POST` (cabeçalho `Idempotency-Key`).
- **Limitação de requisições** (rate limiting).
- **Health Check de readiness** (`/health/ready`, com verificação do Oracle).
- **Testes de integração** dos endpoints.

---

## 🛟 Solução de Problemas

| Sintoma | Causa provável | Solução |
|---|---|---|
| `ORA-00955: name is already used by an existing object` ao rodar a migration | As tabelas `DB_*` já existem no seu usuário Oracle | Apague as tabelas antigas (`DROP TABLE ... CASCADE CONSTRAINTS`) e rode `dotnet ef database update --project Colabs.API` |
| `ORA-01017` (usuário/senha inválidos) ou `ORA-12154` | Credenciais ou `Data Source` do Oracle ausentes ou incorretos | Preencha `ConnectionStrings:OracleConnection` no `appsettings.json` e **reinicie a API** |
| A API não sobe: `Configuração 'Jwt:Key' ausente ou curta demais` | Falta a seção `Jwt` no `appsettings.json` (ou chave com menos de 32 caracteres) | Adicione a seção `Jwt` (veja o [passo 2](#2-configure-as-credenciais-no-appsettingsjson-obrigatório)) |
| `You must install or update .NET to run this application` (`Microsoft.AspNetCore.App 8.0.0`) | Só há runtime 9/10 instalado | Instale o **.NET 8 SDK** (`winget install Microsoft.DotNet.SDK.8`) |
| `dotnet ef` não é reconhecido | A ferramenta do EF Core não está instalada | `dotnet tool install --global dotnet-ef --version 8.0.11` e reabra o terminal |
| `401 Unauthorized` em uma rota protegida | Token ausente, inválido ou expirado | Faça login de novo em `POST /api/Auth/login` e use *Authorize* no Swagger |
| `403 Forbidden` em categorias ou instituições (escrita) | O usuário não é `ADMIN` | Promova o usuário (veja o [passo 8](#8-crie-o-primeiro-administrador)) e **faça login de novo** |
| O usuário foi promovido a `ADMIN`, mas ainda recebe `403` | O papel vai dentro do token, e o token antigo continua `USER` | Faça login de novo para obter um token com o novo papel |
| `409` ao remover usuário, categoria, instituição ou pedido | O registro está em uso (vínculos) ou é o último administrador | Remova ou cancele os vínculos antes; para pedidos e doações, use a mudança de status |
| `409` ao avaliar uma doação | A doação não está `Delivered` ou já foi avaliada | Confira o status da doação e se já existe uma avaliação (`GET /api/Review?idDonation={id}`) |
| `400` no cadastro por CPF ou CNPJ inválido | Os dígitos verificadores não conferem | Use um documento válido (aceita com ou sem pontuação) |
| Erro de certificado HTTPS ao chamar a API | Certificado de desenvolvimento não confiável | `dotnet dev-certs https --trust` (ou use `-k` no `curl`) |
