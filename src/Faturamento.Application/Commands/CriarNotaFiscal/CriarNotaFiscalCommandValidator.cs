using FluentValidation;

namespace Faturamento.Application.Commands.CriarNotaFiscal;

public sealed class CriarNotaFiscalCommandValidator : AbstractValidator<CriarNotaFiscalCommand>
{
    public CriarNotaFiscalCommandValidator()
    {
        RuleFor(c => c.Itens)
            .NotEmpty().WithMessage("A nota fiscal deve conter ao menos um item.");

        RuleForEach(c => c.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.CodigoProduto)
                .NotEmpty().WithMessage("Codigo do produto e obrigatorio.");

            item.RuleFor(i => i.Quantidade)
                .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero.");
        });
    }
}
