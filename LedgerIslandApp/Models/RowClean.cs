namespace LedgerIslandApp.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("RowsClean", Schema = "li")]
public class RowClean
{
    [Key] public long CleanId { get; set; }

    public Guid BatchId { get; set; }
    public UploadBatch Batch { get; set; } = default!;

    public long RowId { get; set; }
    public RowRaw Row { get; set; } = default!;

    public int SrcRowNum { get; set; }

    [MaxLength(256)] public string? Account { get; set; }
    [MaxLength(2000)] public string? Description { get; set; }
    public DateTime? PostingDate { get; set; } // map to DATE in SQL with Fluent API
    public decimal? Amount { get; set; }
    [MaxLength(16)] public string? Currency { get; set; }
    [MaxLength(256)] public string? Source { get; set; }

    [Column(TypeName = "binary(32)")] public byte[]? HashKey { get; set; }

    public DateTime CleanedAtUtc { get; set; } = DateTime.UtcNow;
}
