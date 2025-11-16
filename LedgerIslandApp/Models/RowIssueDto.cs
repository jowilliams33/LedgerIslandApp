namespace LedgerIslandApp.Models;

public sealed record RowIssueDto(
    long CleanRowId,
    string Severity,          // e.g. "Info" | "Warn" | "Error"
    string Code,              // short machine code, e.g. "TB_UNBAL"
    string? Field,            // optional field name (e.g. "Debit")
    string Message            // human-readable detail
);
