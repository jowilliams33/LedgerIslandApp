namespace LedgerIslandApp.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("RowsRaw", Schema = "li")]
public class RowRaw
{
    [Key] public long RowId { get; set; }

    public Guid BatchId { get; set; }
    public UploadBatch Batch { get; set; } = default!;

    public int RowNum { get; set; }

    [Required] public string RawLine { get; set; } = default!;

    [MaxLength(4000)] public string? Col1 { get; set; }
    [MaxLength(4000)] public string? Col2 { get; set; }
    [MaxLength(4000)] public string? Col3 { get; set; }
    [MaxLength(4000)] public string? Col4 { get; set; }
    [MaxLength(4000)] public string? Col5 { get; set; }

    public DateTime IngestedAtUtc { get; set; } = DateTime.UtcNow;

    public RowClean? Clean { get; set; } // 1–1 convenience
}
