using LedgerIslandApp.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LedgerIslandApp.Services
{
    public class ImportService 
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ImportService> _log;

        public ImportService(AppDbContext db, ILogger<ImportService> log)
        {
            _db = db;
            _log = log;
        }

        public Task<int> ImportAsync(IEnumerable<string[]> rows, string[] headers)
            => Task.FromResult(rows?.Count() ?? 0); // stub for now

        /// <summary>
        /// Calls the stored procedure ledger.PostBatchToGolden
        /// to post clean rows for a batch to ledger.Golden.
        /// </summary>
        public async Task PostToGoldenAsync(Guid batchId)
        {
            var pBatch = new SqlParameter("@BatchId", batchId);
            var pApprovedBy = new SqlParameter("@ApprovedBy", "system"); // TODO: inject current user later

            var rows = await _db.Database.ExecuteSqlRawAsync(
                "EXEC ledger.PostBatchToGolden @BatchId, @ApprovedBy",
                pBatch, pApprovedBy
            );

            _log.LogInformation(
                "Stored proc posted {Rows} rows from batch {BatchId} to ledger.Golden",
                rows, batchId
            );
        }
    }
}
