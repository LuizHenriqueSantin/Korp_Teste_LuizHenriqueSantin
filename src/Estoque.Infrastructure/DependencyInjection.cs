using Estoque.Application.Interfaces;
using Estoque.Infrastructure.Data;
using Estoque.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Estoque.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EstoqueDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("EstoqueDb")));

        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        return services;
    }
}
