using System.Data;
using System.Text.Json;
using HRM.ErrorLogs.Models;
using HRM.ErrorLogs.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HRM.Services.Diagnostics;

/// <summary>
/// Persists ErrorLogEntry records to the dedicated HrmErrorLogs SQL Server database (ERR_ErrorLogs table).
/// </summary>
public sealed class SqlErrorLogStore : IErrorLogStore
{
    private readonly string _connectionString;
    private readonly ILogger<SqlErrorLogStore>? _logger;
    private static bool _schemaEnsured;
    private static readonly object _schemaLock = new();

    public SqlErrorLogStore(
        IConfiguration configuration,
        ILogger<SqlErrorLogStore>? logger = null,
        string? connectionString = null)
    {
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _connectionString = connectionString;
        }
        else
        {
            var raw = configuration.GetConnectionString("ErrorLogsConnection") ??
                      configuration.GetConnectionString("HrmErrorLogs");

            if (string.IsNullOrWhiteSpace(raw))
            {
                var fallback = configuration.GetConnectionString("DefaultConnection") ??
                               "Server=DESKTOP-IGBGLUQ\\SQLEXPRESS;Database=HrmErrorLogs;Trusted_Connection=True;TrustServerCertificate=True;";
                try
                {
                    var csb = new SqlConnectionStringBuilder(fallback) { InitialCatalog = "HrmErrorLogs" };
                    raw = csb.ConnectionString;
                }
                catch
                {
                    raw = "Server=DESKTOP-IGBGLUQ\\SQLEXPRESS;Database=HrmErrorLogs;Trusted_Connection=True;TrustServerCertificate=True;";
                }
            }
            _connectionString = raw;
        }

        EnsureDatabaseAndSchema();
    }

    public async Task PersistAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry == null) return;

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
INSERT INTO [dbo].[ERR_ErrorLogs] (
    [Id], [TimestampUtc], [TimestampLocal], [CriticalityLevel], [ApplicationSource],
    [Environment], [MachineName], [ProcessId], [ThreadId], [TraceId],
    [Username], [UserId], [RemoteIp], [PageLocation], [HttpMethod],
    [StatusCode], [UserAgent], [ExceptionType], [ErrorMessage], [StackTrace],
    [InnerExceptionType], [InnerErrorMessage], [InnerStackTrace],
    [FileName], [FilePath], [LineNumber], [ColumnNumber], [ClassName],
    [MethodName], [Namespace], [AssemblyName], [RootCauseSummary],
    [RecommendedAction], [DiagnosticStepsJson], [SuggestedCodePattern],
    [CustomDataJson], [IsResolved], [ResolvedBy], [ResolvedAt], [ResolutionNotes],
    [IsDeleted], [DeletedBy], [DeletedAt]
) VALUES (
    @Id, @TimestampUtc, @TimestampLocal, @CriticalityLevel, @ApplicationSource,
    @Environment, @MachineName, @ProcessId, @ThreadId, @TraceId,
    @Username, @UserId, @RemoteIp, @PageLocation, @HttpMethod,
    @StatusCode, @UserAgent, @ExceptionType, @ErrorMessage, @StackTrace,
    @InnerExceptionType, @InnerErrorMessage, @InnerStackTrace,
    @FileName, @FilePath, @LineNumber, @ColumnNumber, @ClassName,
    @MethodName, @Namespace, @AssemblyName, @RootCauseSummary,
    @RecommendedAction, @DiagnosticStepsJson, @SuggestedCodePattern,
    @CustomDataJson, @IsResolved, @ResolvedBy, @ResolvedAt, @ResolutionNotes,
    @IsDeleted, @DeletedBy, @DeletedAt
);";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", entry.Id);
            cmd.Parameters.AddWithValue("@TimestampUtc", entry.TimestampUtc);
            cmd.Parameters.AddWithValue("@TimestampLocal", entry.TimestampLocal);
            cmd.Parameters.AddWithValue("@CriticalityLevel", (int)entry.CriticalityLevel);
            cmd.Parameters.AddWithValue("@ApplicationSource", entry.ApplicationSource ?? "HRM.Api");
            cmd.Parameters.AddWithValue("@Environment", entry.Environment ?? "Development");
            cmd.Parameters.AddWithValue("@MachineName", entry.MachineName ?? System.Environment.MachineName);
            cmd.Parameters.AddWithValue("@ProcessId", entry.ProcessId);
            cmd.Parameters.AddWithValue("@ThreadId", entry.ThreadId);
            cmd.Parameters.AddWithValue("@TraceId", (object?)entry.TraceId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Username", (object?)entry.Username ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserId", (object?)entry.UserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RemoteIp", (object?)entry.RemoteIp ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PageLocation", (object?)entry.PageLocation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HttpMethod", (object?)entry.HttpMethod ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StatusCode", (object?)entry.StatusCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserAgent", (object?)entry.UserAgent ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ExceptionType", entry.ExceptionType ?? string.Empty);
            cmd.Parameters.AddWithValue("@ErrorMessage", entry.ErrorMessage ?? string.Empty);
            cmd.Parameters.AddWithValue("@StackTrace", (object?)entry.StackTrace ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InnerExceptionType", (object?)entry.InnerExceptionType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InnerErrorMessage", (object?)entry.InnerErrorMessage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InnerStackTrace", (object?)entry.InnerStackTrace ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FileName", (object?)entry.Location?.FileName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FilePath", (object?)entry.Location?.FilePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LineNumber", entry.Location?.LineNumber ?? 0);
            cmd.Parameters.AddWithValue("@ColumnNumber", entry.Location?.ColumnNumber ?? 0);
            cmd.Parameters.AddWithValue("@ClassName", (object?)entry.Location?.ClassName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MethodName", (object?)entry.Location?.MethodName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Namespace", (object?)entry.Location?.Namespace ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AssemblyName", (object?)entry.Location?.AssemblyName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RootCauseSummary", (object?)entry.Fix?.RootCauseSummary ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RecommendedAction", (object?)entry.Fix?.RecommendedAction ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DiagnosticStepsJson", entry.Fix?.DiagnosticSteps != null ? JsonSerializer.Serialize(entry.Fix.DiagnosticSteps) : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SuggestedCodePattern", (object?)entry.Fix?.SuggestedCodePattern ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CustomDataJson", (object?)entry.CustomDataJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsResolved", entry.IsResolved);
            cmd.Parameters.AddWithValue("@ResolvedBy", (object?)entry.ResolvedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ResolvedAt", (object?)entry.ResolvedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ResolutionNotes", (object?)entry.ResolutionNotes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsDeleted", entry.IsDeleted);
            cmd.Parameters.AddWithValue("@DeletedBy", (object?)entry.DeletedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DeletedAt", (object?)entry.DeletedAt ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SqlErrorLogStore: Unable to write to HrmErrorLogs database (ERR_ErrorLogs table).");
        }
    }

    public async Task PersistBatchAsync(IReadOnlyList<ErrorLogEntry> entries, CancellationToken cancellationToken = default)
    {
        if (entries == null || entries.Count == 0) return;

        if (entries.Count == 1)
        {
            await PersistAsync(entries[0], cancellationToken);
            return;
        }

        try
        {
            var table = BuildErrorLogsDataTable();
            foreach (var entry in entries)
            {
                var row = table.NewRow();
                PopulateRow(row, entry);
                table.Rows.Add(row);
            }

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            using var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.CheckConstraints, null)
            {
                DestinationTableName = "[dbo].[ERR_ErrorLogs]",
                BatchSize = entries.Count,
                BulkCopyTimeout = 30
            };

            foreach (DataColumn col in table.Columns)
            {
                bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(table, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SqlErrorLogStore: Unable to write batch of {Count} errors to HrmErrorLogs database.", entries.Count);
        }
    }

    private static DataTable BuildErrorLogsDataTable()
    {
        var table = new DataTable("ERR_ErrorLogs");
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("TimestampUtc", typeof(DateTime));
        table.Columns.Add("TimestampLocal", typeof(DateTime));
        table.Columns.Add("CriticalityLevel", typeof(int));
        table.Columns.Add("ApplicationSource", typeof(string));
        table.Columns.Add("Environment", typeof(string));
        table.Columns.Add("MachineName", typeof(string));
        table.Columns.Add("ProcessId", typeof(int));
        table.Columns.Add("ThreadId", typeof(int));
        table.Columns.Add("TraceId", typeof(string));
        table.Columns.Add("Username", typeof(string));
        table.Columns.Add("UserId", typeof(string));
        table.Columns.Add("RemoteIp", typeof(string));
        table.Columns.Add("PageLocation", typeof(string));
        table.Columns.Add("HttpMethod", typeof(string));
        table.Columns.Add("StatusCode", typeof(int));
        table.Columns.Add("UserAgent", typeof(string));
        table.Columns.Add("ExceptionType", typeof(string));
        table.Columns.Add("ErrorMessage", typeof(string));
        table.Columns.Add("StackTrace", typeof(string));
        table.Columns.Add("InnerExceptionType", typeof(string));
        table.Columns.Add("InnerErrorMessage", typeof(string));
        table.Columns.Add("InnerStackTrace", typeof(string));
        table.Columns.Add("FileName", typeof(string));
        table.Columns.Add("FilePath", typeof(string));
        table.Columns.Add("LineNumber", typeof(int));
        table.Columns.Add("ColumnNumber", typeof(int));
        table.Columns.Add("ClassName", typeof(string));
        table.Columns.Add("MethodName", typeof(string));
        table.Columns.Add("Namespace", typeof(string));
        table.Columns.Add("AssemblyName", typeof(string));
        table.Columns.Add("RootCauseSummary", typeof(string));
        table.Columns.Add("RecommendedAction", typeof(string));
        table.Columns.Add("DiagnosticStepsJson", typeof(string));
        table.Columns.Add("SuggestedCodePattern", typeof(string));
        table.Columns.Add("CustomDataJson", typeof(string));
        table.Columns.Add("IsResolved", typeof(bool));
        table.Columns.Add("ResolvedBy", typeof(string));
        table.Columns.Add("ResolvedAt", typeof(DateTime));
        table.Columns.Add("ResolutionNotes", typeof(string));
        table.Columns.Add("IsDeleted", typeof(bool));
        table.Columns.Add("DeletedBy", typeof(string));
        table.Columns.Add("DeletedAt", typeof(DateTime));
        return table;
    }

    private static void PopulateRow(DataRow row, ErrorLogEntry entry)
    {
        row["Id"] = entry.Id;
        row["TimestampUtc"] = entry.TimestampUtc;
        row["TimestampLocal"] = entry.TimestampLocal;
        row["CriticalityLevel"] = (int)entry.CriticalityLevel;
        row["ApplicationSource"] = entry.ApplicationSource ?? "HRM.Api";
        row["Environment"] = entry.Environment ?? "Development";
        row["MachineName"] = entry.MachineName ?? System.Environment.MachineName;
        row["ProcessId"] = entry.ProcessId;
        row["ThreadId"] = entry.ThreadId;
        row["TraceId"] = (object?)entry.TraceId ?? DBNull.Value;
        row["Username"] = (object?)entry.Username ?? DBNull.Value;
        row["UserId"] = (object?)entry.UserId ?? DBNull.Value;
        row["RemoteIp"] = (object?)entry.RemoteIp ?? DBNull.Value;
        row["PageLocation"] = (object?)entry.PageLocation ?? DBNull.Value;
        row["HttpMethod"] = (object?)entry.HttpMethod ?? DBNull.Value;
        row["StatusCode"] = (object?)entry.StatusCode ?? DBNull.Value;
        row["UserAgent"] = (object?)entry.UserAgent ?? DBNull.Value;
        row["ExceptionType"] = entry.ExceptionType;
        row["ErrorMessage"] = entry.ErrorMessage;
        row["StackTrace"] = (object?)entry.StackTrace ?? DBNull.Value;
        row["InnerExceptionType"] = (object?)entry.InnerExceptionType ?? DBNull.Value;
        row["InnerErrorMessage"] = (object?)entry.InnerErrorMessage ?? DBNull.Value;
        row["InnerStackTrace"] = (object?)entry.InnerStackTrace ?? DBNull.Value;
        row["FileName"] = (object?)entry.Location?.FileName ?? DBNull.Value;
        row["FilePath"] = (object?)entry.Location?.FilePath ?? DBNull.Value;
        row["LineNumber"] = (object?)entry.Location?.LineNumber ?? DBNull.Value;
        row["ColumnNumber"] = (object?)entry.Location?.ColumnNumber ?? DBNull.Value;
        row["ClassName"] = (object?)entry.Location?.ClassName ?? DBNull.Value;
        row["MethodName"] = (object?)entry.Location?.MethodName ?? DBNull.Value;
        row["Namespace"] = (object?)entry.Location?.Namespace ?? DBNull.Value;
        row["AssemblyName"] = (object?)entry.Location?.AssemblyName ?? DBNull.Value;
        row["RootCauseSummary"] = (object?)entry.Fix?.RootCauseSummary ?? DBNull.Value;
        row["RecommendedAction"] = (object?)entry.Fix?.RecommendedAction ?? DBNull.Value;
        row["DiagnosticStepsJson"] = entry.Fix?.DiagnosticSteps != null ? JsonSerializer.Serialize(entry.Fix.DiagnosticSteps) : DBNull.Value;
        row["SuggestedCodePattern"] = (object?)entry.Fix?.SuggestedCodePattern ?? DBNull.Value;
        row["CustomDataJson"] = (object?)entry.CustomDataJson ?? DBNull.Value;
        row["IsResolved"] = entry.IsResolved;
        row["ResolvedBy"] = (object?)entry.ResolvedBy ?? DBNull.Value;
        row["ResolvedAt"] = (object?)entry.ResolvedAt ?? DBNull.Value;
        row["ResolutionNotes"] = (object?)entry.ResolutionNotes ?? DBNull.Value;
        row["IsDeleted"] = entry.IsDeleted;
        row["DeletedBy"] = (object?)entry.DeletedBy ?? DBNull.Value;
        row["DeletedAt"] = (object?)entry.DeletedAt ?? DBNull.Value;
    }

    public async Task<IReadOnlyList<ErrorLogEntry>> LoadRecentAsync(
        int count,
        bool includeDeleted = false,
        bool onlyDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var list = new List<ErrorLogEntry>();
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            string filterClause = onlyDeleted
                ? "WHERE [IsDeleted] = 1"
                : includeDeleted ? "" : "WHERE [IsDeleted] = 0";

            var sql = $"SELECT TOP ({Math.Min(count, 500)}) * FROM [dbo].[ERR_ErrorLogs] {filterClause} ORDER BY [TimestampUtc] DESC;";
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(MapEntryFromReader(reader));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SqlErrorLogStore: Unable to read from HrmErrorLogs database (ERR_ErrorLogs table).");
        }

        return list;
    }

    public async Task<ErrorLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = "SELECT TOP (1) * FROM [dbo].[ERR_ErrorLogs] WHERE [Id] = @Id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return MapEntryFromReader(reader);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SqlErrorLogStore: Unable to read error log {Id} from HrmErrorLogs database.", id);
        }

        return null;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, string deletedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
UPDATE [dbo].[ERR_ErrorLogs]
SET [IsDeleted] = 1,
    [DeletedBy] = @DeletedBy,
    [DeletedAt] = SYSUTCDATETIME()
WHERE [Id] = @Id AND [IsDeleted] = 0;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@DeletedBy", (object?)deletedBy ?? "Developer");

            int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SqlErrorLogStore: Unable to soft delete error log {Id}.", id);
            return false;
        }
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
UPDATE [dbo].[ERR_ErrorLogs]
SET [IsDeleted] = 0,
    [DeletedBy] = NULL,
    [DeletedAt] = NULL
WHERE [Id] = @Id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SqlErrorLogStore: Unable to restore error log {Id}.", id);
            return false;
        }
    }

    public async Task<int> SoftDeleteResolvedAsync(string deletedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
UPDATE [dbo].[ERR_ErrorLogs]
SET [IsDeleted] = 1,
    [DeletedBy] = @DeletedBy,
    [DeletedAt] = SYSUTCDATETIME()
WHERE [IsResolved] = 1 AND [IsDeleted] = 0;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@DeletedBy", (object?)deletedBy ?? "Developer");

            return await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SqlErrorLogStore: Unable to soft delete resolved error logs from HrmErrorLogs database.");
            return 0;
        }
    }

    private static ErrorLogEntry MapEntryFromReader(SqlDataReader reader)
    {
        int colIsDeleted = -1;
        try { colIsDeleted = reader.GetOrdinal("IsDeleted"); } catch { }
        int colDeletedBy = -1;
        try { colDeletedBy = reader.GetOrdinal("DeletedBy"); } catch { }
        int colDeletedAt = -1;
        try { colDeletedAt = reader.GetOrdinal("DeletedAt"); } catch { }

        var entry = new ErrorLogEntry
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            TimestampUtc = reader.GetDateTime(reader.GetOrdinal("TimestampUtc")),
            TimestampLocal = reader.GetDateTime(reader.GetOrdinal("TimestampLocal")),
            CriticalityLevel = (ErrorCriticalityLevel)reader.GetInt32(reader.GetOrdinal("CriticalityLevel")),
            ApplicationSource = reader.GetString(reader.GetOrdinal("ApplicationSource")),
            Environment = reader.GetString(reader.GetOrdinal("Environment")),
            MachineName = reader.GetString(reader.GetOrdinal("MachineName")),
            ProcessId = reader.GetInt32(reader.GetOrdinal("ProcessId")),
            ThreadId = reader.GetInt32(reader.GetOrdinal("ThreadId")),
            TraceId = reader.IsDBNull(reader.GetOrdinal("TraceId")) ? null : reader.GetString(reader.GetOrdinal("TraceId")),
            Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? null : reader.GetString(reader.GetOrdinal("Username")),
            UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetString(reader.GetOrdinal("UserId")),
            RemoteIp = reader.IsDBNull(reader.GetOrdinal("RemoteIp")) ? null : reader.GetString(reader.GetOrdinal("RemoteIp")),
            PageLocation = reader.IsDBNull(reader.GetOrdinal("PageLocation")) ? null : reader.GetString(reader.GetOrdinal("PageLocation")),
            HttpMethod = reader.IsDBNull(reader.GetOrdinal("HttpMethod")) ? null : reader.GetString(reader.GetOrdinal("HttpMethod")),
            StatusCode = reader.IsDBNull(reader.GetOrdinal("StatusCode")) ? null : reader.GetInt32(reader.GetOrdinal("StatusCode")),
            UserAgent = reader.IsDBNull(reader.GetOrdinal("UserAgent")) ? null : reader.GetString(reader.GetOrdinal("UserAgent")),
            ExceptionType = reader.GetString(reader.GetOrdinal("ExceptionType")),
            ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
            StackTrace = reader.IsDBNull(reader.GetOrdinal("StackTrace")) ? null : reader.GetString(reader.GetOrdinal("StackTrace")),
            InnerExceptionType = reader.IsDBNull(reader.GetOrdinal("InnerExceptionType")) ? null : reader.GetString(reader.GetOrdinal("InnerExceptionType")),
            InnerErrorMessage = reader.IsDBNull(reader.GetOrdinal("InnerErrorMessage")) ? null : reader.GetString(reader.GetOrdinal("InnerErrorMessage")),
            InnerStackTrace = reader.IsDBNull(reader.GetOrdinal("InnerStackTrace")) ? null : reader.GetString(reader.GetOrdinal("InnerStackTrace")),
            Location = new ErrorLocation
            {
                FileName = reader.IsDBNull(reader.GetOrdinal("FileName")) ? string.Empty : reader.GetString(reader.GetOrdinal("FileName")),
                FilePath = reader.IsDBNull(reader.GetOrdinal("FilePath")) ? string.Empty : reader.GetString(reader.GetOrdinal("FilePath")),
                LineNumber = reader.GetInt32(reader.GetOrdinal("LineNumber")),
                ColumnNumber = reader.GetInt32(reader.GetOrdinal("ColumnNumber")),
                ClassName = reader.IsDBNull(reader.GetOrdinal("ClassName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ClassName")),
                MethodName = reader.IsDBNull(reader.GetOrdinal("MethodName")) ? string.Empty : reader.GetString(reader.GetOrdinal("MethodName")),
                Namespace = reader.IsDBNull(reader.GetOrdinal("Namespace")) ? string.Empty : reader.GetString(reader.GetOrdinal("Namespace")),
                AssemblyName = reader.IsDBNull(reader.GetOrdinal("AssemblyName")) ? string.Empty : reader.GetString(reader.GetOrdinal("AssemblyName"))
            },
            Fix = new FixRecommendation
            {
                RootCauseSummary = reader.IsDBNull(reader.GetOrdinal("RootCauseSummary")) ? string.Empty : reader.GetString(reader.GetOrdinal("RootCauseSummary")),
                RecommendedAction = reader.IsDBNull(reader.GetOrdinal("RecommendedAction")) ? string.Empty : reader.GetString(reader.GetOrdinal("RecommendedAction")),
                SuggestedCodePattern = reader.IsDBNull(reader.GetOrdinal("SuggestedCodePattern")) ? null : reader.GetString(reader.GetOrdinal("SuggestedCodePattern"))
            },
            CustomDataJson = reader.IsDBNull(reader.GetOrdinal("CustomDataJson")) ? null : reader.GetString(reader.GetOrdinal("CustomDataJson")),
            IsResolved = reader.GetBoolean(reader.GetOrdinal("IsResolved")),
            ResolvedBy = reader.IsDBNull(reader.GetOrdinal("ResolvedBy")) ? null : reader.GetString(reader.GetOrdinal("ResolvedBy")),
            ResolvedAt = reader.IsDBNull(reader.GetOrdinal("ResolvedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ResolvedAt")),
            ResolutionNotes = reader.IsDBNull(reader.GetOrdinal("ResolutionNotes")) ? null : reader.GetString(reader.GetOrdinal("ResolutionNotes")),
            IsDeleted = colIsDeleted >= 0 && !reader.IsDBNull(colIsDeleted) && reader.GetBoolean(colIsDeleted),
            DeletedBy = colDeletedBy >= 0 && !reader.IsDBNull(colDeletedBy) ? reader.GetString(colDeletedBy) : null,
            DeletedAt = colDeletedAt >= 0 && !reader.IsDBNull(colDeletedAt) ? reader.GetDateTime(colDeletedAt) : null
        };

        var stepsJson = reader.IsDBNull(reader.GetOrdinal("DiagnosticStepsJson")) ? null : reader.GetString(reader.GetOrdinal("DiagnosticStepsJson"));
        if (!string.IsNullOrWhiteSpace(stepsJson))
        {
            try
            {
                entry.Fix.DiagnosticSteps = JsonSerializer.Deserialize<List<string>>(stepsJson) ?? new List<string>();
            }
            catch { }
        }

        return entry;
    }

    public async Task<bool> UpdateResolutionAsync(Guid id, bool isResolved, string? resolvedBy, string? notes, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
UPDATE [dbo].[ERR_ErrorLogs]
SET [IsResolved] = @IsResolved,
    [ResolvedBy] = @ResolvedBy,
    [ResolvedAt] = @ResolvedAt,
    [ResolutionNotes] = @ResolutionNotes
WHERE [Id] = @Id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@IsResolved", isResolved);
            cmd.Parameters.AddWithValue("@ResolvedBy", (object?)resolvedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ResolvedAt", isResolved ? DateTime.UtcNow : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ResolutionNotes", (object?)notes ?? DBNull.Value);

            int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SqlErrorLogStore: Unable to update resolution in HrmErrorLogs for {Id}", id);
            return false;
        }
    }

    private void EnsureDatabaseAndSchema()
    {
        if (_schemaEnsured) return;

        lock (_schemaLock)
        {
            if (_schemaEnsured) return;

            try
            {
                // 1. Ensure dedicated HrmErrorLogs database exists on the target SQL Server
                try
                {
                    var masterCsb = new SqlConnectionStringBuilder(_connectionString)
                    {
                        InitialCatalog = "master",
                        ConnectTimeout = 8
                    };

                    using (var masterConn = new SqlConnection(masterCsb.ConnectionString))
                    {
                        masterConn.Open();
                        using var cmdDb = new SqlCommand(
                            "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'HrmErrorLogs') CREATE DATABASE [HrmErrorLogs];",
                            masterConn);
                        cmdDb.ExecuteNonQuery();
                    }
                }
                catch (Exception exMaster)
                {
                    _logger?.LogDebug(exMaster, "SqlErrorLogStore: Master database check skipped or non-privileged.");
                }

                // 2. Ensure ERR_ErrorLogs table and indexes exist in HrmErrorLogs database
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    const string ddl = @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ERR_ErrorLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ERR_ErrorLogs](
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_ERR_ErrorLogs] PRIMARY KEY ([Id]),
        [TimestampUtc] DATETIME2(7) NOT NULL,
        [TimestampLocal] DATETIME2(7) NOT NULL,
        [CriticalityLevel] INT NOT NULL,
        [ApplicationSource] NVARCHAR(100) NOT NULL,
        [Environment] NVARCHAR(50) NOT NULL,
        [MachineName] NVARCHAR(100) NOT NULL,
        [ProcessId] INT NOT NULL,
        [ThreadId] INT NOT NULL,
        [TraceId] NVARCHAR(150) NULL,
        [Username] NVARCHAR(250) NULL,
        [UserId] NVARCHAR(150) NULL,
        [RemoteIp] NVARCHAR(100) NULL,
        [PageLocation] NVARCHAR(1000) NULL,
        [HttpMethod] NVARCHAR(20) NULL,
        [StatusCode] INT NULL,
        [UserAgent] NVARCHAR(1000) NULL,
        [ExceptionType] NVARCHAR(300) NOT NULL,
        [ErrorMessage] NVARCHAR(MAX) NOT NULL,
        [StackTrace] NVARCHAR(MAX) NULL,
        [InnerExceptionType] NVARCHAR(300) NULL,
        [InnerErrorMessage] NVARCHAR(MAX) NULL,
        [InnerStackTrace] NVARCHAR(MAX) NULL,
        [FileName] NVARCHAR(500) NULL,
        [FilePath] NVARCHAR(1000) NULL,
        [LineNumber] INT NOT NULL CONSTRAINT [DF_ERR_ErrorLogs_LineNumber] DEFAULT (0),
        [ColumnNumber] INT NOT NULL CONSTRAINT [DF_ERR_ErrorLogs_ColumnNumber] DEFAULT (0),
        [ClassName] NVARCHAR(300) NULL,
        [MethodName] NVARCHAR(300) NULL,
        [Namespace] NVARCHAR(300) NULL,
        [AssemblyName] NVARCHAR(300) NULL,
        [RootCauseSummary] NVARCHAR(MAX) NULL,
        [RecommendedAction] NVARCHAR(MAX) NULL,
        [DiagnosticStepsJson] NVARCHAR(MAX) NULL,
        [SuggestedCodePattern] NVARCHAR(MAX) NULL,
        [CustomDataJson] NVARCHAR(MAX) NULL,
        [IsResolved] BIT NOT NULL CONSTRAINT [DF_ERR_ErrorLogs_IsResolved] DEFAULT (0),
        [ResolvedBy] NVARCHAR(250) NULL,
        [ResolvedAt] DATETIME2(7) NULL,
        [ResolutionNotes] NVARCHAR(MAX) NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_ERR_ErrorLogs_IsDeleted] DEFAULT (0),
        [DeletedBy] NVARCHAR(250) NULL,
        [DeletedAt] DATETIME2(7) NULL
    );

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ERR_ErrorLogs]') AND name = N'IX_ERR_ErrorLogs_TimestampUtc')
        CREATE NONCLUSTERED INDEX [IX_ERR_ErrorLogs_TimestampUtc] ON [dbo].[ERR_ErrorLogs]([TimestampUtc] DESC);

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ERR_ErrorLogs]') AND name = N'IX_ERR_ErrorLogs_Criticality')
        CREATE NONCLUSTERED INDEX [IX_ERR_ErrorLogs_Criticality] ON [dbo].[ERR_ErrorLogs]([CriticalityLevel], [IsResolved]);

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ERR_ErrorLogs]') AND name = N'IX_ERR_ErrorLogs_ApplicationSource')
        CREATE NONCLUSTERED INDEX [IX_ERR_ErrorLogs_ApplicationSource] ON [dbo].[ERR_ErrorLogs]([ApplicationSource], [TimestampUtc] DESC);

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ERR_ErrorLogs]') AND name = N'IX_ERR_ErrorLogs_IsDeleted')
        CREATE NONCLUSTERED INDEX [IX_ERR_ErrorLogs_IsDeleted] ON [dbo].[ERR_ErrorLogs]([IsDeleted], [TimestampUtc] DESC);
END";

                    using var cmd = new SqlCommand(ddl, conn);
                    cmd.ExecuteNonQuery();
                }

                _schemaEnsured = true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "SqlErrorLogStore: Unable to automatically verify or create ERR_ErrorLogs in HrmErrorLogs database.");
            }
        }
    }
}
