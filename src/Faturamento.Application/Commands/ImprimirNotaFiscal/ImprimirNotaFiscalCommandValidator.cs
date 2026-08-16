using FluentValidation;

namespace Faturamento.Application.Commands.ImprimirNotaFiscal;

public sealed class ImprimirNotaFiscalCommandValidator : AbstractValidator<ImprimirNotaFiscalCommand>
{
    public ImprimirNotaFiscalCommandValidator()
    {
        RuleFor(c => c.NotaFiscalId)
            .GreaterThan(0).WithMessage("Id da nota fiscal invalido.");
    }
}
