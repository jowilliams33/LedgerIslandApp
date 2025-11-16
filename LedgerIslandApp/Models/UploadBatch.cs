namespace LedgerIslandApp.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
[Table("UploadBatch", Schema = "li")]
public class UploadBatch
{
    [Key] public Guid BatchId { get; set; } = Guid.NewGuid();

    [Required, MaxLength(260)] public string FileName { get; set; } = default!;
    [MaxLength(128)] public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }

    [MaxLength(256)] public string? UploadedBy { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(64)] public string? SourceIp { get; set; }

    /// <summary>0=Received, 1=Parsed, 2=Cleaned, 3=Posted</summary>
    public byte Status { get; set; } = 0;

    [MaxLength(1000)] public string? Notes { get; set; }

    public ICollection<RowRaw> RowsRaw { get; set; } = new List<RowRaw>();
    public ICollection<RowClean> RowsClean { get; set; } = new List<RowClean>();
}

