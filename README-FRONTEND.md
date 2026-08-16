# Frontend — Sistema de Emissão de Notas Fiscais

Aplicação em **Angular 22**, standalone (sem `NgModule`), usando **Signals** para estado reativo e **Angular Material** para os componentes visuais. Consome exclusivamente o `Gateway.API` (`http://localhost:5000/api`) — nunca fala diretamente com Estoque ou Faturamento.

## Stack e bibliotecas

| Biblioteca | Finalidade |
|---|---|
| `@angular/core` (Signals, standalone components) | Estado reativo local (`signal()`), componentes sem módulos |
| `@angular/material` + `@angular/cdk` | Componentes visuais (tabela, cards, dialogs, form fields, chips, ícones, spinner) |
| `@angular/animations` | Animações do Material (dialogs, ripple, transições) |
| `@angular/forms` (Reactive Forms) | Formulários tipados (`FormBuilder`, `FormArray`, `Validators`) |
| `rxjs` | Composição de chamadas HTTP e streams assíncronos |
| `@angular/router` | Rotas *standalone*, lazy-loaded por feature |

Não há bibliotecas de terceiros além do próprio ecossistema Angular — decisão deliberada para manter a stack enxuta e 100% alinhada ao que o Angular oferece nativamente.

## Estrutura de pastas

```
src/app/
  core/
    interceptors/      correlation-id-interceptor.ts, error-handler-interceptor.ts
    services/          produto.ts, nota-fiscal.ts, notification.ts
    models/            produto.model.ts, nota-fiscal.model.ts, api-error.model.ts
  shared/components/   data-table, loading-button, confirm-dialog, page-header
  features/
    produtos/          produtos-list, produto-form
    notas-fiscais/     notas-list, nota-form, nota-detalhe
  app.ts / app.html / app.scss     shell (toolbar + router-outlet)
  app.config.ts                    providers (HttpClient, interceptors, router, animations)
  app.routes.ts                    definição de rotas
```

`core` = infraestrutura transversal (HTTP, notificações, contratos de dados). `shared` = componentes de UI **reutilizáveis e agnósticos de domínio** (não sabem o que é "produto" ou "nota"). `features` = telas de negócio, cada uma consumindo `core` e `shared`.

## Interceptors HTTP (funcionais)

Angular moderno usa **interceptors funcionais** (`HttpInterceptorFn`), registrados em `app.config.ts`:
```ts
provideHttpClient(withInterceptors([correlationIdInterceptor, errorHandlerInterceptor]))
```
A ordem no array define a ordem de execução na ida (request) e a ordem inversa na volta (response/erro) — como uma pilha de middlewares.

**`correlationIdInterceptor`** — gera um `crypto.randomUUID()` por requisição e o anexa via `req.clone({ setHeaders: {...} })` (`HttpRequest` é imutável, por isso `clone`). É o **ponto de origem** da cadeia de rastreabilidade que o backend propaga entre Gateway → Faturamento → Estoque (ver `README-BACKEND.md`).

**`errorHandlerInterceptor`** — único ponto de tratamento de erro HTTP de toda a aplicação. Usa `catchError`/`throwError` do RxJS para: (1) reconhecer erros de negócio no formato `{ errors: [{chave, mensagem}] }` retornado pelo `MainController.CustomResponse` do backend e mostrar a mensagem real; (2) tratar `status === 0` (rede indisponível, CORS, ou circuit breaker do Polly aberto no backend) com mensagem de indisponibilidade; (3) qualquer outro status, mensagem genérica com o código HTTP. Sempre relança o erro (`throwError`) depois de notificar, para que os componentes ainda consigam reagir (ex.: desligar um spinner via `finalize`).

## Uso de RxJS

Não é só "o `HttpClient` retorna Observable" — há operadores aplicados deliberadamente:

- **`map` + `switchMap`** (`nota-detalhe.ts`) — reage a mudanças no parâmetro de rota (`:id`) e encadeia a busca da nota. `switchMap` cancela automaticamente uma requisição anterior se o usuário navegar para outra nota antes dela responder.
- **`filter` + `tap` + `switchMap` (encadeado 2x) + `finalize`** — o fluxo de impressão: `filter` interrompe se o usuário cancelar o dialog de confirmação; `tap` liga o spinner como efeito colateral; dois `switchMap` sequenciais chamam primeiro `POST /imprimir` e, só depois de concluído, um novo `GET` para buscar a nota já atualizada; `finalize` desliga o spinner independentemente de sucesso ou erro.
- **`catchError` + `throwError`** (`error-handler-interceptor.ts`) — tratamento centralizado de erros.
- **`finalize`** (`produto-form.ts`, `nota-form.ts`) — desliga o estado de "salvando" após a chamada HTTP, sucesso ou erro.

Nenhuma `Promise`/`async-await` é usada no código de produção — tudo é modelado como stream RxJS, inclusive os efeitos colaterais de UI.

## Ciclos de vida do Angular utilizados

- **`ngOnInit`** — usado em `produtos-list.ts` (carrega a lista ao montar), `nota-form.ts` (carrega produtos disponíveis para o formulário) e `nota-detalhe.ts` (assina o parâmetro de rota). Preferido a fazer a chamada no construtor porque o DI e os bindings de `@Input`/route params já estão resolvidos nesse ponto do ciclo de vida.
- **Signals como alternativa a `ngOnChanges`** — os componentes usam `input()` (signal inputs) ao invés de `@Input()` decorado; mudanças de valor são lidas via `computed()`/leitura direta do signal na *template*, sem precisar implementar `ngOnChanges` manualmente.
- Não há necessidade de `ngOnDestroy` para `unsubscribe` manual: todas as subscriptions HTTP completam sozinhas (um único valor emitido) e o Angular gerencia o ciclo de vida do `ActivatedRoute.paramMap` automaticamente ao destruir o componente.

## Componentes visuais — Angular Material

Escolhido como biblioteca de UI por ser a solução oficial do time Angular, com tema **Material 3** (`mat.theme()` em `styles.scss`, paleta `azure` primária / `blue` terciária). Componentes usados: `MatTable` (via `data-table` genérico), `MatCard`, `MatDialog`, `MatFormField`/`MatInput`/`MatSelect`, `MatButton`, `MatIcon`, `MatChips`, `MatProgressSpinner`, `MatSnackBar`.

## Componentes reutilizáveis (`shared/`)

- **`DataTable<T extends object>`** — tabela genérica: recebe `columns: TableColumn<T>[]` (chave + label + formatador opcional) e `data: T[]`, monta o `mat-table` dinamicamente via `*ngFor` sobre `matColumnDef` (padrão suportado pelo Angular Material para colunas definidas em runtime). Suporta uma coluna de ações opcional via projeção de conteúdo (`<ng-template #actions>`) e clique de linha (`rowClick`). Usado tanto em `produtos-list` quanto em `notas-list` e `nota-detalhe`, sem duplicar HTML de tabela em nenhum lugar.
- **`LoadingButton`** — botão com spinner embutido, usado em qualquer ação assíncrona (salvar produto, criar nota, imprimir nota) — implementa o requisito "exibir indicador de processamento".
- **`ConfirmDialog`** — dialog de confirmação genérico (título + mensagem + labels customizáveis), usado antes de qualquer ação irreversível (imprimir a nota).
- **`PageHeader`** — cabeçalho padrão de página (título + subtítulo + slot de ações via `ng-content`), garante consistência visual entre todas as telas.

## Models (`core/models`)

Interfaces TypeScript puras (zero overhead em runtime, apagadas na compilação) espelhando os DTOs reais do backend:
- `Produto` / `CriarProdutoRequest` — espelham `ProdutoDto` / `CriarProdutoCommand`.
- `StatusNotaFiscal` (`'Aberta' | 'Fechada'`) — union de string literal, não enum numérico, porque o backend serializa o enum C# como string (`JsonStringEnumConverter`).
- `ItemNotaFiscal` (leitura) vs `ItemRequest` (escrita) — mesma forma, tipos separados por representarem momentos diferentes do fluxo, evitando acoplamento acidental caso um dos dois ganhe campos extras no futuro.
- `ApiErrorResponse` / `ApiErrorItem` — espelham o formato `{ errors: [{chave, mensagem}] }` do `MainController.CustomResponse`, usado no `errorHandlerInterceptor`.

## Services (`core/services`)

Usam o novo decorator **`@Service()`** do Angular 22 (equivalente a `@Injectable({ providedIn: 'root' })`, mas exige DI via `inject()` — não suporta injeção por construtor):
- **`ProdutoService`** / **`NotaFiscalService`** — encapsulam todas as chamadas HTTP (`GET`/`POST`) ao Gateway, retornando `Observable<T>` tipado pelos models.
- **`NotificationService`** — centraliza feedback visual (`MatSnackBar`), com métodos `sucesso()`, `erro()` e `errosDeApi()` (junta múltiplas mensagens de negócio em uma única notificação).

## Funcionalidades implementadas

- **Cadastro de Produtos** (`produtos-list` + dialog `produto-form`) — código, descrição, saldo inicial.
- **Cadastro de Notas Fiscais** (`nota-form`) — `FormArray` dinâmico para múltiplos itens (produto + quantidade), numeração e status `Aberta` definidos pelo backend.
- **Impressão de Notas** (`nota-detalhe`) — botão com `LoadingButton` (indicador de processamento), `ConfirmDialog` antes de confirmar (ação irreversível), bloqueado se a nota não estiver `Aberta`, e após sucesso: snackbar de confirmação + `window.print()` para o diálogo nativo do navegador (impressão real do documento, com folha de estilos dedicada para impressão via `@media print` escondendo botões e mantendo só o conteúdo da nota).
- **Feedback de falhas** — qualquer erro do backend (validação de negócio, indisponibilidade de serviço, erro inesperado) chega ao usuário via snackbar colorido (verde/vermelho), nunca uma tela quebrada ou console silencioso.

## Como rodar localmente

Pré-requisitos: Node 22+, os 3 serviços de backend rodando (`README-BACKEND.md`).

```powershell
cd frontend
npm install
npm start        # ng serve, http://localhost:4200
```

A aplicação espera o `Gateway.API` em `http://localhost:5000` (configurável em `src/environments/environment.ts`).

## Requisitos do desafio — mapeamento

| Requisito | Onde |
|---|---|
| Cadastro de Produtos | `features/produtos` |
| Cadastro de Notas Fiscais (numeração seq., status Aberta, múltiplos itens) | `features/notas-fiscais/nota-form` |
| Botão de impressão intuitivo + indicador de processamento | `nota-detalhe` + `LoadingButton` |
| Bloqueio de impressão para notas não-Abertas | validação em `nota-detalhe.imprimir()` antes de chamar a API (espelha a mesma regra do backend) |
| Feedback de erro ao usuário | `errorHandlerInterceptor` + `NotificationService` (snackbar) |
| Componentes reutilizáveis | `DataTable`, `LoadingButton`, `ConfirmDialog`, `PageHeader` |
