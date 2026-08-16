using Estoque.Application.Notifications;
using FluentValidation;
using MediatR;

namespace Estoque.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly IMediator _mediator;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, IMediator mediator)
    {
        _validators = validators;
        _mediator = mediator;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var falhas = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(erro => erro is not null)
            .ToList();

        if (falhas.Count == 0)
        {
            return await next(cancellationToken);
        }

        foreach (var falha in falhas)
        {
            await _mediator.Publish(
                new DomainNotification(falha.PropertyName, falha.ErrorMessage),
                cancellationToken);
        }

        return default!;
    }
}
