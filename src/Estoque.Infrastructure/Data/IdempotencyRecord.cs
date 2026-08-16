namespace Estoque.Infrastructure.Data;

public class IdempotencyRecord
{
    public string Key { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
}
