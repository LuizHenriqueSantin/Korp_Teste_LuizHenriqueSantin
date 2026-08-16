using FluentValidation;

namespace Estoque.Application.Commands.CriarProduto;

public sealed class CriarProdutoCommandValidator : AbstractValidator<CriarProdutoCommand>
{
    public CriarProdutoCommandValidator()
    {
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("Codigo e obrigatorio.")
            .MaximumLength(30).WithMessage("Codigo deve ter no maximo 30 caracteres.");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descricao e obrigatoria.")
            .MaximumLength(200).WithMessage("Descricao deve ter no maximo 200 caracteres.");

        RuleFor(x => x.SaldoInicial)
            .GreaterThanOrEqualTo(0).WithMessage("Saldo inicial nao pode ser negativo.");
    }
}
