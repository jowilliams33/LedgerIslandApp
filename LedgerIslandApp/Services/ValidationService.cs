using System.Data;
using System.Text;
using LedgerIslandApp.Data;
using LedgerIslandApp.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LedgerIslandApp.Services
{
    public class ValidationService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ValidationService> _log;

        public ValidationService(AppDbContext db, ILogger<ValidationService> log)
        {
            _db = db;
            _log = log;
        }

        // ----------------------------------------
        // INTERNAL: build issues for a batch
        // ----------------------------------------
        private async Task PopulateAsync(Guid batchId)
        {
            await using var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "val.PopulateIssuesForBatch";
            cmd.CommandType = CommandType.StoredProcedure;

            var p = cmd.CreateParameter();
            p.ParameterName = "@BatchId";
            p.Value = batchId;
            cmd.Parameters.Add(p);

            await cmd.ExecuteNonQueryAsync();
        }

        // ----------------------------------------
        // PUBLIC: get issues (DTO-friendly)
        // ----------------------------------------
        public async Task<IReadOnlyList<RowIssueDto>> GetIssuesAsync(Guid batchId)
        {
            // 1) Build/refresh issues
            await PopulateAsync(batchId);

            // 2) Fetch them
            var result = new List<RowIssueDto>();

            await using var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "val.GetIssuesForBatch";
            cmd.CommandType = CommandType.StoredProcedure;

            var pBatch = cmd.CreateParameter();
            pBatch.ParameterName = "@BatchId";
            pBatch.Value = batchId;
            cmd.Parameters.Add(pBatch);

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                result.Add(new RowIssueDto(
                    CleanRowId: rdr.GetInt64(0),
                    Severity: rdr.GetString(1),
                    Code: rdr.GetString(2),
                    Field: rdr.IsDBNull(3) ? null : rdr.GetString(3),
                    Message: rdr.GetString(4)
                ));
            }

            _log.LogInformation("Fetched {Count} issues for batch {BatchId}", result.Count, batchId);
            return result;
        }

        // NEW: wrapper used by Imports.razor
        public Task<IReadOnlyList<RowIssueDto>> GetIssuesByBatchAsync(Guid batchId)
            => GetIssuesAsync(batchId);

        // ----------------------------------------
        // PUBLIC: batch totals
        // ----------------------------------------
        public async Task<BatchTotalsDto?> GetTotalsAsync(Guid batchId)
        {
            await using var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "val.GetBatchTotals";
            cmd.CommandType = CommandType.StoredProcedure;

            var p = cmd.CreateParameter();
            p.ParameterName = "@BatchId";
            p.Value = batchId;
            cmd.Parameters.Add(p);

            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;

            return new BatchTotalsDto(
                RowCount: r.IsDBNull(0) ? 0 : r.GetInt32(0),
                TotalDebit: r.IsDBNull(1) ? 0 : r.GetDecimal(1),
                TotalCredit: r.IsDBNull(2) ? 0 : r.GetDecimal(2),
                MissingAmountRows: r.IsDBNull(3) ? 0 : r.GetInt32(3),
                BothSidesRows: r.IsDBNull(4) ? 0 : r.GetInt32(4)
            );
        }

        // ----------------------------------------
        // PUBLIC: post to golden (strict 2-param proc)
        // Returns number of rows inserted in THIS run.
        // ----------------------------------------
        public async Task<int> PostToGoldenAsync(Guid batchId, string approvedBy)
        {
            await using var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "ledger.PostBatchToGolden"; // strict: (@BatchId UNIQUEIDENTIFIER, @ApprovedBy NVARCHAR(128))
            cmd.CommandType = CommandType.StoredProcedure;

            var pBatch = cmd.CreateParameter();
            pBatch.ParameterName = "@BatchId";
            pBatch.Value = batchId;
            cmd.Parameters.Add(pBatch);

            var pBy = cmd.CreateParameter();
            pBy.ParameterName = "@ApprovedBy";
            pBy.Value = approvedBy ?? "system";
            cmd.Parameters.Add(pBy);

            // The proc SELECTs: RowsInsertedThisRun, TotalRowsInGolden
            await using var rdr = await cmd.ExecuteReaderAsync();
            int insertedThisRun = 0;
            if (await rdr.ReadAsync() && !rdr.IsDBNull(0))
                insertedThisRun = rdr.GetInt32(0);

            _log.LogInformation("Posted batch {BatchId}. Rows inserted this run: {Rows}", batchId, insertedThisRun);
            return insertedThisRun;
        }

        // ----------------------------------------
        // PUBLIC: latest post audit entry for a batch
        // ----------------------------------------
        public async Task<PostAuditDto?> GetLastPostAsync(Guid batchId)
        {
            const string sql = @"
                SELECT TOP (1)
                    BatchId, ApprovedBy, PostedAtUtc, RunRows, TotalRows
                FROM ledger.PostAudit
                WHERE BatchId = @BatchId
                ORDER BY AuditId DESC;";

            await using var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;

            var p = cmd.CreateParameter();
            p.ParameterName = "@BatchId";
            p.Value = batchId;
            cmd.Parameters.Add(p);

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return null;

            return new PostAuditDto(
                BatchId: rdr.GetGuid(0),
                ApprovedBy: rdr.GetString(1),
                PostedAtUtc: rdr.GetDateTime(2),
                RunRows: rdr.GetInt32(3),
                TotalRows: rdr.GetInt32(4)
            );
        }
    }
}
