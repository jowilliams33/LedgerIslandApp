namespace LedgerIslandApp.Models
{
    public sealed record BatchTotalsDto(
        int RowCount,
        decimal TotalDebit,
        decimal TotalCredit,
        int MissingAmountRows,
        int BothSidesRows
    );
}
