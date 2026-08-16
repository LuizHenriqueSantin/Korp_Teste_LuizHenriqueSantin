using Faturamento.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faturamento.Infrastructure.Data.Configurations;

public class ItemNotaFiscalConfiguration : IEntityTypeConfiguration<ItemNotaFiscal>
{
    public void Configure(EntityTypeBuilder<ItemNotaFiscal> builder)
    {
        builder.ToTable("ItensNotaFiscal");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.CodigoProduto)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(i => i.Quantidade)
            .IsRequired();
    }
}
