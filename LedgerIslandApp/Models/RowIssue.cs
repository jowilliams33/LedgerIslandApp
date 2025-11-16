namespace LedgerIslandApp.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("RowIssues", Schema = "li")]
public class RowIssue
{
    [Key] public long IssueId { get; set; }

    public Guid BatchId { get; set; }

    public long? RowId { get; set; }
    public long? CleanId { get; set; }

    public byte Severity { get; set; } // 1=Info,2=Warn,3=Error

    [MaxLength(64)] public string Code { get; set; } = default!;
    [MaxLength(2000)] public string Message { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
