namespace LedgerIslandApp.Models;

public sealed record PostAuditDto(
    Guid BatchId,
    string ApprovedBy,
    DateTime PostedAtUtc,
    int RunRows,
    int TotalRows
);
