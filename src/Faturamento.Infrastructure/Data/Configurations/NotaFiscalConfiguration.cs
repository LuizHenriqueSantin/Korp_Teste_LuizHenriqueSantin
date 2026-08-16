using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faturamento.Infrastructure.Data.Configurations;

public class NotaFiscalConfiguration : IEntityTypeConfiguration<NotaFiscal>
{
    public void Configure(EntityTypeBuilder<NotaFiscal> builder)
    {
        builder.ToTable("NotasFiscais");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Numero)
            .HasDefaultValueSql("NEXT VALUE FOR NotaFiscalNumeroSequence")
            .ValueGeneratedOnAdd();

        builder.HasIndex(n => n.Numero).IsUnique();

        builder.Property(n => n.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(n => n.DataCriacaoUtc).IsRequired();
        builder.Property(n => n.DataFechamentoUtc);

        builder.HasMany(n => n.Itens)
            .WithOne()
            .HasForeignKey(i => i.NotaFiscalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(n => n.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
