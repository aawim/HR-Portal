using HRM.ErrorLogs.Diagnostics;
using HRM.ErrorLogs.Models;
using HRM.ErrorLogs.Services;
using Xunit;

namespace HRM.Api.UnitTests.Diagnostics;

public sealed class ErrorLogsTests
{
    [Fact]
    public void StackTraceAnalyzer_ExtractsLocationAndLineNumber_FromThrownException()
    {
        // Arrange
        Exception caughtException;
        try
        {
            ThrowTestExceptionWithKnownLine();
            throw new Exception("Unreachable");
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Act
        var location = StackTraceAnalyzer.Analyze(caughtException);

        // Assert
        Assert.NotNull(location);
        Assert.Contains("ErrorLogsTests.cs", location.FileName);
        Assert.True(location.LineNumber > 0, $"Expected LineNumber > 0, but got {location.LineNumber}");
        Assert.Equal(nameof(ThrowTestExceptionWithKnownLine), location.MethodName);
        Assert.Equal(nameof(ErrorLogsTests), location.ClassName);
        Assert.True(location.HasLineNumber);
    }

    [Fact]
    public void FixRecommendationEngine_GeneratesPrescriptiveFix_ForNullReferenceException()
    {
        // Arrange
        var ex = new NullReferenceException("Object reference not set to an instance of an object.");
        var location = new ErrorLocation
        {
            FileName = "PayrollProcessor.cs",
            LineNumber = 142,
            MethodName = "ProcessRunBatchesAsync"
        };

        // Act
        var fix = FixRecommendationEngine.Generate(ex, location);

        // Assert
        Assert.NotNull(fix);
        Assert.Contains("line 142 in PayrollProcessor.cs", fix.RootCauseSummary);
        Assert.Contains("null-conditional", fix.RecommendedAction, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(fix.DiagnosticSteps);
        Assert.NotNull(fix.SuggestedCodePattern);
    }

    [Fact]
    public void FixRecommendationEngine_GeneratesPrescriptiveFix_ForSqlUniqueConstraintViolation()
    {
        // Arrange
        var ex = new InvalidOperationException("Violation of UNIQUE KEY constraint 'UQ_PAY_Runs'. Cannot insert duplicate key.");
        var location = new ErrorLocation { FileName = "EnterprisePayrollService.cs", LineNumber = 88 };

        // Act
        var fix = FixRecommendationEngine.Generate(ex, location);

        // Assert
        Assert.NotNull(fix);
        Assert.Contains("unique key violation", fix.RootCauseSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("uniqueness", fix.RecommendedAction, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(fix.SuggestedCodePattern);
    }

    [Fact]
    public async Task ErrorLogService_CapturesAllRequiredFields_IncludingLocationAndFix()
    {
        // Arrange
        var service = new ErrorLogService(persistentStore: null, logger: null, defaultApplicationSource: "HRM.Api");
        var context = new ErrorContext
        {
            Username = "ahmed.developer",
            UserId = "USR-2026-DEV-01",
            RemoteIp = "192.168.1.55",
            PageLocation = "/api/v1/payroll/queue/claim",
            HttpMethod = "POST",
            StatusCode = 500,
            ApplicationSource = "HRM.Api",
            TraceId = "trace-test-12345"
        };

        Exception thrownEx;
        try
        {
            throw new ArgumentNullException("jobId", "Job identifier cannot be null.");
        }
        catch (Exception ex)
        {
            thrownEx = ex;
        }

        // Act
        var entry = await service.CaptureExceptionAsync(thrownEx, ErrorCriticalityLevel.Error, context);

        // Assert
        Assert.NotNull(entry);
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal("ahmed.developer", entry.Username);
        Assert.Equal("USR-2026-DEV-01", entry.UserId);
        Assert.Equal("192.168.1.55", entry.RemoteIp);
        Assert.Equal("/api/v1/payroll/queue/claim", entry.PageLocation);
        Assert.Equal("POST", entry.HttpMethod);
        Assert.Equal(500, entry.StatusCode);
        Assert.Equal(ErrorCriticalityLevel.Error, entry.CriticalityLevel);
        Assert.Equal("HRM.Api", entry.ApplicationSource);
        Assert.Equal("trace-test-12345", entry.TraceId);
        Assert.Equal(typeof(ArgumentNullException).FullName, entry.ExceptionType);

        // Location & Line Number Assertions
        Assert.NotNull(entry.Location);
        Assert.Equal("ErrorLogsTests.cs", entry.Location.FileName);
        Assert.True(entry.Location.LineNumber > 0);

        // Fix Recommendation Assertions
        Assert.NotNull(entry.Fix);
        Assert.False(string.IsNullOrWhiteSpace(entry.Fix.RootCauseSummary));
        Assert.False(string.IsNullOrWhiteSpace(entry.Fix.RecommendedAction));
        Assert.NotEmpty(entry.Fix.DiagnosticSteps);
    }

    [Fact]
    public async Task ErrorLogService_HandlesResolutionLifecycle_Correctly()
    {
        // Arrange
        var service = new ErrorLogService(persistentStore: null, logger: null, defaultApplicationSource: "HRM.Api");
        var entry = await service.CaptureExceptionAsync(new InvalidOperationException("Test lifecycle exception"));

        // Act 1: Mark Resolved
        var resolved = await service.MarkResolvedAsync(entry.Id, "SeniorDev", "Fixed by updating query");
        var fetched1 = await service.GetErrorByIdAsync(entry.Id);

        // Assert 1
        Assert.True(resolved);
        Assert.NotNull(fetched1);
        Assert.True(fetched1.IsResolved);
        Assert.Equal("SeniorDev", fetched1.ResolvedBy);
        Assert.Equal("Fixed by updating query", fetched1.ResolutionNotes);

        // Act 2: Reopen
        var reopened = await service.ReopenAsync(entry.Id);
        var fetched2 = await service.GetErrorByIdAsync(entry.Id);

        // Assert 2
        Assert.True(reopened);
        Assert.NotNull(fetched2);
        Assert.False(fetched2.IsResolved);
        Assert.Null(fetched2.ResolvedBy);
    }

    [Fact]
    public async Task ErrorLogService_ComputesStatistics_Accurately()
    {
        // Arrange
        var service = new ErrorLogService(persistentStore: null, logger: null, defaultApplicationSource: "HRM.Api");

        await service.CaptureExceptionAsync(new NullReferenceException(), ErrorCriticalityLevel.Critical, new ErrorContext { ApplicationSource = "HRM.Api" });
        await service.CaptureExceptionAsync(new InvalidOperationException(), ErrorCriticalityLevel.Error, new ErrorContext { ApplicationSource = "HRM.UI" });
        await service.CaptureExceptionAsync(new TimeoutException(), ErrorCriticalityLevel.Warning, new ErrorContext { ApplicationSource = "HRM.Payroll.Worker" });

        // Act
        var stats = await service.GetStatisticsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.TotalErrors >= 3);
        Assert.True(stats.CriticalCount >= 1);
        Assert.True(stats.ErrorCount >= 1);
        Assert.True(stats.WarningCount >= 1);
        Assert.True(stats.SourceBreakdown.ContainsKey("HRM.Api"));
        Assert.True(stats.SourceBreakdown.ContainsKey("HRM.UI"));
        Assert.True(stats.SourceBreakdown.ContainsKey("HRM.Payroll.Worker"));
    }

    private static void ThrowTestExceptionWithKnownLine()
    {
        throw new InvalidOperationException("Known test exception thrown for stack trace testing.");
    }
}
