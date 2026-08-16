# Backend — Sistema de Emissão de Notas Fiscais

Solução em **.NET 10**, organizada como **arquitetura de microsserviços**, com um API Gateway na frente de dois serviços independentes: **Estoque** (controle de produtos e saldos) e **Faturamento** (gestão de notas fiscais).

## Visão geral da arquitetura

```
                        ┌─────────────────────┐
   Angular (4200) ───▶  │   Gateway.API :5000  │  (YARP Reverse Proxy)
                        └──────────┬───────────┘
                     /api/estoque/*│  │*/api/faturamento/*
                                   ▼  ▼
                 ┌───────────────────┐   ┌─────────────────────┐
                 │  Estoque.API :5101 │   │ Faturamento.API :5201│
                 │  (produtos/saldo)  │◀──│ (notas fiscais)      │
                 └─────────┬─────────┘   └──────────┬───────────┘
                           │                          │
                     SQL Server                  SQL Server
              NotasFiscais_Estoque          NotasFiscais_Faturamento
```

- O **navegador só conversa com o Gateway** (porta 5000). Toda comunicação entre Faturamento e Estoque acontece **server-to-server** via `HttpClient`, nunca exposta ao browser.
- Cada serviço é dono do seu próprio banco de dados (um banco por microsserviço), reforçando o isolamento — não há *joins* nem *foreign keys* entre bancos.
- Cada microsserviço segue a mesma organização em camadas: `*.Domain` (entidades e regras de negócio puras), `*.Application` (casos de uso via MediatR, DTOs, validações), `*.Infrastructure` (EF Core, repositórios, clientes HTTP), `*.API` (controllers, middlewares, composição/DI).

## Projetos da solução

| Projeto | Porta | Responsabilidade |
|---|---|---|
| `Gateway.API` | 5000 | Reverse proxy (YARP), CORS, correlation-id, ponto único de entrada |
| `Estoque.API` | 5101 | Cadastro de produtos, controle de saldo, idempotência de débito |
| `Faturamento.API` | 5201 | Cadastro/emissão de notas fiscais, orquestração da impressão |

## Gateway (YARP)

O `Gateway.API` usa **YARP (Yet Another Reverse Proxy)**, configurado 100% via `appsettings.json` (sem código de roteamento):

```json
"Routes": {
  "estoque-route": { "Match": { "Path": "/api/estoque/{**catch-all}" }, "Transforms": [{ "PathRemovePrefix": "/api/estoque" }] },
  "faturamento-route": { "Match": { "Path": "/api/faturamento/{**catch-all}" }, "Transforms": [{ "PathRemovePrefix": "/api/faturamento" }] }
}
```

Requisições para `/api/estoque/**` são roteadas para `http://localhost:5101`, e `/api/faturamento/**` para `http://localhost:5201`, com o prefixo removido antes de repassar. O Gateway também expõe:
- **CORS** — única camada da solução que precisa da política (`AngularDev`, liberando `http://localhost:4200`), já que é o único ponto que o navegador acessa diretamente.
- **Correlation-Id** — middleware que gera/propaga o header `X-Correlation-Id` (ver seção dedicada abaixo).

## MediatR + CQRS

Cada camada `*.Application` usa **MediatR** para separar comandos (escrita) e queries (leitura), com handlers dedicados por caso de uso:

```
Commands/
  CriarNotaFiscal/
    CriarNotaFiscalCommand.cs
    CriarNotaFiscalCommandHandler.cs
    CriarNotaFiscalCommandValidator.cs   (FluentValidation)
  ImprimirNotaFiscal/
    ImprimirNotaFiscalCommandHandler.cs
Queries/
  ObterNotaFiscalPorId/ ...
```

Um `ValidationBehavior<TRequest, TResponse>` (em `Behaviors/`) é registrado como *pipeline behavior* do MediatR e roda automaticamente **antes** de qualquer handler, validando o request via **FluentValidation** e curto-circuitando o fluxo se houver erros — os controllers e handlers não precisam chamar validação manualmente.

## Tratamento de erros e exceções — `DomainNotification`

Ao invés de lançar exceções para erros de regra de negócio (ex.: "saldo insuficiente", "nota já fechada"), a solução usa o padrão **Notification Pattern**, implementado com o próprio pipeline do MediatR:

```csharp
public class DomainNotification : INotification
{
    public string Chave { get; }
    public string Mensagem { get; }
}
```

`DomainNotification` implementa `INotification` do MediatR — isso permite que qualquer handler simplesmente faça `_mediator.Publish(new DomainNotification(...))` e o `DomainNotificationHandler` (um `INotificationHandler<DomainNotification>` com escopo por requisição) **acumula** as notificações em memória durante a execução daquele request. O controller então consulta esse handler:

```csharp
protected IActionResult CustomResponse(object? result = null)
{
    if (OperacaoValida())            // !_notifications.HasNotifications()
        return result is null ? NoContent() : Ok(result);

    return BadRequest(new { errors = _notifications.GetNotifications().Select(n => new { n.Chave, n.Mensagem }) });
}
```

Vantagens sobre `try/catch` com exceções: erros de negócio **não geram stack unwinding nem custo de exceção**, múltiplas notificações podem ser acumuladas na mesma operação (ex.: vários produtos com saldo insuficiente na mesma nota), e o controller nunca precisa de blocos `try/catch` — só verifica o estado ao final via `CustomResponse`.

**Exceções de verdade** (bugs, falhas de infraestrutura, banco fora do ar) continuam sendo tratadas separadamente por um middleware global (`ExceptionHandlingMiddleware`) em cada `*.API`, que captura qualquer `Exception` não tratada e responde com `ProblemDetails` (RFC 7807) padronizado, status 500, logando com `TraceId` para correlação.

## Persistência — Entity Framework Core + SQL Server

Cada serviço usa **EF Core** com **SQL Server (LocalDB)**, um `DbContext` próprio por serviço:
- `EstoqueDbContext` → banco `NotasFiscais_Estoque`
- `FaturamentoDbContext` → banco `NotasFiscais_Faturamento`

Migrations aplicadas via `dotnet ef database update` em cada projeto de API. Repositórios (`ProdutoRepository`, `NotaFiscalRepository`) encapsulam todo acesso a dados — os handlers do MediatR nunca usam o `DbContext` diretamente, apenas as interfaces (`IProdutoRepository`, `INotaFiscalRepository`) definidas em `*.Application/Interfaces`.

### Uso de LINQ

LINQ é usado extensivamente na camada `Infrastructure` (queries) e `Application` (transformação de dados):
```csharp
_context.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct);
_context.Produtos.Where(p => lista.Contains(p.Codigo)).ToListAsync(ct);
_context.Produtos.AsNoTracking().ToListAsync(ct);          // leitura sem tracking, mais performático
notaFiscal.Itens.Select(i => new ItemNotaFiscalDto(i.CodigoProduto, i.Quantidade)).ToList();
```
- `Where` + `Contains` para buscar múltiplos produtos de uma vez por código (usado no débito de saldo em lote).
- `AsNoTracking()` em toda leitura que não vai ser modificada (listagens), evitando o overhead do change tracker do EF Core.
- `Select`/`ToList` para mapear entidades de domínio em DTOs de saída sem expor a entidade diretamente na API.

## Tratamento de concorrência — `RowVersion` (opcional, implementado)

O `Produto` tem uma coluna `RowVersion` (`byte[]`), mapeada como **concurrency token** do EF Core (`[Timestamp]`/`IsRowVersion()`), que o SQL Server gerencia automaticamente — é incrementada a cada `UPDATE` na linha.

Cenário do desafio (saldo=1 debitado simultaneamente por duas notas): as duas transações leem o mesmo `RowVersion`; a primeira grava e o SQL Server muda o valor; quando a segunda tenta salvar, o EF Core inclui o `RowVersion` original na cláusula `WHERE` do `UPDATE` — como não bate mais, **zero linhas são afetadas**, e o EF Core lança `DbUpdateConcurrencyException`. O `ProdutoRepository` captura essa exceção, recarrega a entidade (`entry.ReloadAsync`) e relança como uma `ConcurrencyConflictException` de domínio, que sobe como uma `DomainNotification` amigável ao usuário — nenhuma das duas notas debita saldo indevidamente, e a que perder a corrida recebe feedback claro para tentar novamente.

## Resiliência entre serviços — Polly

A comunicação **Faturamento → Estoque** (débito de saldo) usa `HttpClientFactory` + **Polly**, com três políticas encadeadas no `IEstoqueApiClient`:

```csharp
services.AddHttpClient<IEstoqueApiClient, EstoqueApiClient>(client => { client.BaseAddress = new Uri(estoqueBaseUrl); })
    .AddHttpMessageHandler<CorrelationIdHandler>()
    .AddPolicyHandler(GetRetryPolicy())            // retry exponencial: 3 tentativas (2s, 4s, 8s)
    .AddPolicyHandler(GetCircuitBreakerPolicy())   // abre após 5 falhas seguidas, 30s de "descanso"
    .AddPolicyHandler(GetTimeoutPolicy());         // timeout de 10s por chamada
```

- **Retry**: `HandleTransientHttpError()` cobre 5xx e falhas de rede; `WaitAndRetryAsync` com backoff exponencial evita martelar um serviço já sobrecarregado.
- **Circuit Breaker**: depois de 5 falhas consecutivas, "abre o circuito" por 30s — chamadas subsequentes falham **imediatamente** (sem nem tentar a rede), dando tempo do Estoque se recuperar e evitando efeito cascata.
- **Timeout**: garante que uma chamada travada não prenda a requisição do Faturamento indefinidamente.

Essa é a implementação concreta do requisito **"Tratamento de Falhas"**: se o Estoque.API cair, o Faturamento tenta novamente, depois abre o circuito, e o erro chega ao usuário via `DomainNotification` → `errors` no corpo da resposta → snackbar no Angular — nunca uma exceção crua ou tela em branco.

## Falha após débito, antes do fechamento (cenário crítico)

Ponto delicado do fluxo de impressão: e se o débito no Estoque tiver sucesso, mas o `SaveChangesAsync` que fecha a nota falhar (ex.: banco do Faturamento cair nesse instante)? O `ImprimirNotaFiscalCommandHandler` trata esse caso explicitamente:

```csharp
catch (Exception ex)
{
    _logger.LogCritical(ex, "Falha ao persistir o fechamento... A nota permanece Aberta; " +
        "uma nova tentativa de impressao e segura e ira apenas concluir o fechamento.");
    // publica DomainNotification informando o usuário e pedindo para tentar de novo
}
```
A nota fica com status `Aberta` (não foi salva como fechada), mas o saldo já foi debitado de verdade no Estoque. Sem idempotência, uma nova tentativa de impressão debitaria o saldo **de novo** — é exatamente para isso que existe a idempotência abaixo.

## Idempotência (opcional, implementado)

Toda chamada de débito de saldo carrega uma `idempotencyKey` determinística: `$"nota-fiscal-{notaFiscal.Id}"`. No Estoque.API, o `IdempotencyService` verifica, **dentro da mesma transação** do débito, se aquela chave já foi processada (tabela `IdempotencyRecords`):

```csharp
Task<bool> JaProcessadoAsync(string idempotencyKey, ...) => _context.IdempotencyRecords.AnyAsync(x => x.Key == idempotencyKey, ct);
void MarcarComoProcessado(string idempotencyKey) => _context.IdempotencyRecords.Add(new IdempotencyRecord { Key = idempotencyKey, ProcessedAtUtc = DateTime.UtcNow });
```

Se a mesma nota for "impressa" duas vezes (por causa do cenário de falha acima, ou por um duplo-clique do usuário, ou por um retry do Polly), a segunda tentativa de débito é reconhecida como já processada e **não debita o saldo novamente** — resolve exatamente o requisito opcional "operações repetidas não causam efeitos colaterais indesejados".

## Correlation-Id — rastreabilidade ponta a ponta

Todos os três serviços registram um middleware que lê (ou gera) o header `X-Correlation-Id` e o injeta no contexto de log via Serilog:

```csharp
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue("X-Correlation-Id", out var v) ? v.ToString() : Guid.NewGuid().ToString();
    context.Request.Headers["X-Correlation-Id"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        await next();
});
```

O Angular já gera esse header na origem (interceptor `correlationIdInterceptor`). O Gateway propaga o mesmo valor para o serviço de destino. Quando o **Faturamento chama o Estoque**, o `CorrelationIdHandler` (um `DelegatingHandler` registrado via `.AddHttpMessageHandler<CorrelationIdHandler>()`) lê o Id do `IHttpContextAccessor` da requisição atual e o anexa na chamada de saída — por isso o `Faturamento.API` registra `AddHttpContextAccessor()` explicitamente. O resultado: **um único Correlation-Id percorre Angular → Gateway → Faturamento → Estoque**, aparecendo no log de todos, permitindo seguir uma operação específica através dos 4 processos.

## Logging estruturado — Serilog

Todos os serviços usam **Serilog** com output formatado incluindo o `CorrelationId` em toda linha de log:
```
{Timestamp:HH:mm:ss} [{Level:u3}] [CorrelationId:{CorrelationId}] {Message:lj}{NewLine}{Exception}
```

## Como rodar localmente

Pré-requisitos: .NET 10 SDK, SQL Server LocalDB (ou ajustar `ConnectionStrings` nos `appsettings.json`).

```powershell
# 1. Aplicar migrations (a partir da raiz da solução)
dotnet ef database update --project src/Estoque.Infrastructure --startup-project src/Estoque.API
dotnet ef database update --project src/Faturamento.Infrastructure --startup-project src/Faturamento.API

# 2. Subir os 3 serviços (em terminais separados)
dotnet run --project src/Estoque.API        # http://localhost:5101
dotnet run --project src/Faturamento.API    # http://localhost:5201
dotnet run --project src/Gateway.API        # http://localhost:5000  <- ponto de entrada para o frontend
```

O Angular consome exclusivamente `http://localhost:5000/api/...` (ver `README-FRONTEND.md`).

## Requisitos do desafio — mapeamento

| Requisito | Onde |
|---|---|
| Cadastro de Produtos | `Estoque.API` (`ProdutosController`, `CriarProdutoCommand`) |
| Cadastro de Notas Fiscais (numeração sequencial, status Aberta) | `Faturamento.API` (`NotasFiscaisController`, `CriarNotaFiscalCommand`) |
| Impressão (fecha nota + debita saldo) | `ImprimirNotaFiscalCommandHandler`, orquestra chamada ao Estoque via `IEstoqueApiClient` |
| Arquitetura de microsserviços (mín. 2) | `Estoque.API` + `Faturamento.API`, atrás de `Gateway.API` |
| Tratamento de falhas | Polly (retry/circuit breaker/timeout) + `DomainNotification` + snackbar no Angular |
| Conexão real com banco de dados | EF Core + SQL Server, um banco por serviço |
| Concorrência (opcional) | `RowVersion` + `DbUpdateConcurrencyException` no `Produto` |
| Idempotência (opcional) | `IdempotencyRecords` por `idempotencyKey` no débito de saldo |
