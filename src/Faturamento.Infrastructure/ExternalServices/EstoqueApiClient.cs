using System.Net.Http.Json;
using System.Text.Json;
using Faturamento.Application.DTOs;
using Faturamento.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Faturamento.Infrastructure.ExternalServices;

public sealed class EstoqueApiClient : IEstoqueApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly ILogger<EstoqueApiClient> _logger;

    public EstoqueApiClient(HttpClient httpClient, ILogger<EstoqueApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EstoqueDebitoResultado> DebitarSaldoAsync(
        string idempotencyKey,
        IEnumerable<ItemNotaFiscalDto> itens,
        CancellationToken ct = default)
    {
        var payload = new
        {
            idempotencyKey,
            itens = itens.Select(i => new { codigoProduto = i.CodigoProduto, quantidade = i.Quantidade })
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("produtos/debitar-saldo", payload, ct);

            if (response.IsSuccessStatusCode)
            {
                return new EstoqueDebitoResultado(true, null);
            }

            var mensagem = await ExtrairMensagemDeErroAsync(response, ct);
            return new EstoqueDebitoResultado(false, mensagem);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit breaker aberto: Estoque.API esta indisponivel.");
            return new EstoqueDebitoResultado(
                false, "O servico de Estoque esta indisponivel no momento. Tente novamente em instantes.");
        }
        catch (TimeoutRejectedException)
        {
            _logger.LogWarning("Timeout ao chamar o Estoque.API.");
            return new EstoqueDebitoResultado(
                false, "Tempo limite excedido ao comunicar com o servico de Estoque.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha de comunicacao com o Estoque.API.");
            return new EstoqueDebitoResultado(
                false, "Nao foi possivel se comunicar com o servico de Estoque.");
        }
    }

    private static async Task<string> ExtrairMensagemDeErroAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErroResponse>(JsonOptions, ct);
            if (body?.Errors is { Count: > 0 })
            {
                return string.Join(" | ", body.Errors.Select(e => e.Mensagem));
            }
        }
        catch (JsonException) { }

        return $"Estoque retornou {(int)response.StatusCode} ({response.StatusCode}).";
    }

    private sealed record ErroResponse(List<ErroItem> Errors);

    private sealed record ErroItem(string Chave, string Mensagem);
}
