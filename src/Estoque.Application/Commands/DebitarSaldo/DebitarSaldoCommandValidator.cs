using FluentValidation;

namespace Estoque.Application.Commands.DebitarSaldo;

public sealed class DebitarSaldoCommandValidator : AbstractValidator<DebitarSaldoCommand>
{
    public DebitarSaldoCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey e obrigatoria.");

        RuleFor(x => x.Itens)
            .NotEmpty().WithMessage("A lista de itens nao pode ser vazia.");

        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.CodigoProduto).NotEmpty().WithMessage("Codigo do produto e obrigatorio.");
            item.RuleFor(i => i.Quantidade).GreaterThan(0).WithMessage("Quantidade deve ser maior que zero.");
        });
    }
}
