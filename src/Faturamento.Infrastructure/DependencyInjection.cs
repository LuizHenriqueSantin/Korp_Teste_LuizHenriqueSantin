using Faturamento.Application.Interfaces;
using Faturamento.Infrastructure.Data;
using Faturamento.Infrastructure.ExternalServices;
using Faturamento.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Faturamento.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FaturamentoDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("FaturamentoDb")));

        services.AddScoped<INotaFiscalRepository, NotaFiscalRepository>();

        services.AddTransient<CorrelationIdHandler>();

        var estoqueBaseUrl = configuration["EstoqueApi:BaseUrl"]
            ?? throw new InvalidOperationException("Configuracao 'EstoqueApi:BaseUrl' nao encontrada.");

        services.AddHttpClient<IEstoqueApiClient, EstoqueApiClient>(client =>
            {
                client.BaseAddress = new Uri(estoqueBaseUrl);
            })
            .AddHttpMessageHandler<CorrelationIdHandler>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, tentativa => TimeSpan.FromSeconds(Math.Pow(2, tentativa)));

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(30));

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy() =>
        Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10), TimeoutStrategy.Optimistic);
}
