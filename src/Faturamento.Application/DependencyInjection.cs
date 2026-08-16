using Faturamento.Application.Behaviors;
using Faturamento.Application.Notifications;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Faturamento.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.RemoveAll(typeof(INotificationHandler<DomainNotification>));

        services.AddScoped<DomainNotificationHandler>();
        services.AddScoped<IDomainNotificationHandler>(sp => sp.GetRequiredService<DomainNotificationHandler>());
        services.AddScoped<INotificationHandler<DomainNotification>>(sp => sp.GetRequiredService<DomainNotificationHandler>());

        return services;
    }
}
