using System.Data;
using System.Text.Json;
using HRM.ErrorLogs.Models;
using HRM.ErrorLogs.Services;
using Microsoft.Data.SqlClient;

namespace HRM.ErrorLogs.Cli;

public sealed class CliErrorLogStore : IErrorLogStore
{
    private readonly string _connectionString;

    public CliErrorLogStore(string connectionString)
    {
        _connectionString = connectionString;
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
    [CustomDataJson], [IsResolved], [ResolvedBy], [ResolvedAt], [ResolutionNotes]
) VALUES (
    @Id, @TimestampUtc, @TimestampLocal, @CriticalityLevel, @ApplicationSource,
    @Environment, @MachineName, @ProcessId, @ThreadId, @TraceId,
    @Username, @UserId, @RemoteIp, @PageLocation, @HttpMethod,
    @StatusCode, @UserAgent, @ExceptionType, @ErrorMessage, @StackTrace,
    @InnerExceptionType, @InnerErrorMessage, @InnerStackTrace,
    @FileName, @FilePath, @LineNumber, @ColumnNumber, @ClassName,
    @MethodName, @Namespace, @AssemblyName, @RootCauseSummary,
    @RecommendedAction, @DiagnosticStepsJson, @SuggestedCodePattern,
    @CustomDataJson, @IsResolved, @ResolvedBy, @ResolvedAt, @ResolutionNotes
);";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", entry.Id);
            cmd.Parameters.AddWithValue("@TimestampUtc", entry.TimestampUtc);
            cmd.Parameters.AddWithValue("@TimestampLocal", entry.TimestampLocal);
            cmd.Parameters.AddWithValue("@CriticalityLevel", (int)entry.CriticalityLevel);
            cmd.Parameters.AddWithValue("@ApplicationSource", entry.ApplicationSource ?? "HRM.ErrorLogs.CLI");
            cmd.Parameters.AddWithValue("@Environment", entry.Environment ?? "Development");
            cmd.Parameters.AddWithValue("@MachineName", entry.MachineName ?? Environment.MachineName);
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
            cmd.Parameters.AddWithValue("@ExceptionType", entry.ExceptionType);
            cmd.Parameters.AddWithValue("@ErrorMessage", entry.ErrorMessage);
            cmd.Parameters.AddWithValue("@StackTrace", (object?)entry.StackTrace ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InnerExceptionType", (object?)entry.InnerExceptionType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InnerErrorMessage", (object?)entry.InnerErrorMessage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InnerStackTrace", (object?)entry.InnerStackTrace ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FileName", (object?)entry.Location?.FileName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FilePath", (object?)entry.Location?.FilePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LineNumber", (object?)entry.Location?.LineNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ColumnNumber", (object?)entry.Location?.ColumnNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ClassName", (object?)entry.Location?.ClassName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MethodName", (object?)entry.Location?.MethodName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Namespace", (object?)entry.Location?.Namespace ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AssemblyName", (object?)entry.Location?.AssemblyName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RootCauseSummary", (object?)entry.Fix?.RootCauseSummary ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RecommendedAction", (object?)entry.Fix?.RecommendedAction ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DiagnosticStepsJson", entry.Fix?.DiagnosticSteps != null ? JsonSerializer.Serialize(entry.Fix.DiagnosticSteps) : DBNull.Value);
            cmd.Parameters.AddWithValue("@SuggestedCodePattern", (object?)entry.Fix?.SuggestedCodePattern ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CustomDataJson", (object?)entry.CustomDataJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsResolved", entry.IsResolved);
            cmd.Parameters.AddWithValue("@ResolvedBy", (object?)entry.ResolvedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ResolvedAt", (object?)entry.ResolvedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ResolutionNotes", (object?)entry.ResolutionNotes ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLI Store Error] {ex.Message}");
        }
    }

    public async Task PersistBatchAsync(IReadOnlyList<ErrorLogEntry> entries, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
        {
            await PersistAsync(entry, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ErrorLogEntry>> LoadRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var list = new List<ErrorLogEntry>();
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var sql = $"SELECT TOP ({Math.Min(count, 500)}) * FROM [dbo].[ERR_ErrorLogs] ORDER BY [TimestampUtc] DESC;";
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(MapEntry(reader));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLI Store Error] {ex.Message}");
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
                return MapEntry(reader);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLI Store Error] {ex.Message}");
        }

        return null;
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
            Console.WriteLine($"[CLI Store Error] {ex.Message}");
            return false;
        }
    }

    public async Task<int> ClearResolvedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = "DELETE FROM [dbo].[ERR_ErrorLogs] WHERE [IsResolved] = 1;";
            await using var cmd = new SqlCommand(sql, conn);
            return await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLI Store Error] {ex.Message}");
            return 0;
        }
    }

    private static ErrorLogEntry MapEntry(SqlDataReader reader)
    {
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
            ResolutionNotes = reader.IsDBNull(reader.GetOrdinal("ResolutionNotes")) ? null : reader.GetString(reader.GetOrdinal("ResolutionNotes"))
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
}
