using HRM.ErrorLogs.Models;

namespace HRM.ErrorLogs.Diagnostics;

/// <summary>
/// Intelligent diagnostic engine that prescribes root causes and actionable fix recommendations.
/// </summary>
public static class FixRecommendationEngine
{
    public static FixRecommendation Generate(Exception ex, ErrorLocation location)
    {
        var fix = new FixRecommendation();
        if (ex == null)
        {
            fix.RootCauseSummary = "No exception details were provided.";
            fix.RecommendedAction = "Verify caller parameters and error pipeline invocation.";
            return fix;
        }

        var exType = ex.GetType().Name;
        var fullTypeName = ex.GetType().FullName ?? exType;
        var msg = ex.Message ?? string.Empty;
        var locDesc = location.HasLineNumber ? $"at line {location.LineNumber} in {location.FileName}" : (string.IsNullOrWhiteSpace(location.FileName) ? string.Empty : $"in {location.FileName}");

        // 1. NullReferenceException
        if (ex is NullReferenceException || exType.Contains("NullReference", StringComparison.OrdinalIgnoreCase))
        {
            fix.RootCauseSummary = $"An object reference evaluated to 'null' when accessed {locDesc}. This happens when a property, method, or indexer is accessed on a variable that has not been initialized or was returned as null from a query/service.";
            fix.RecommendedAction = $"Inspect {location.FileName} around line {location.LineNumber}. Add null-conditional operators ('?.'), null-coalescing defaults ('??'), or defensive guard clauses.";
            fix.DiagnosticSteps = new List<string>
            {
                $"Open {location.FileName} and navigate to line {location.LineNumber}.",
                "Identify which object, variable, or dependency injection service on that line evaluated to null.",
                "Verify whether database queries (e.g. FindAsync, SingleOrDefaultAsync) returned null due to missing data.",
                "Ensure collections are initialized (e.g. 'new List<T>()') rather than left as null.",
                "Add defensive null-check guards or use the C# null-forgiving operator '!' only if guaranteed non-null."
            };
            fix.SuggestedCodePattern = @"// Example Fix: Defensive null check or fallback
var entity = await _context.Entities.FindAsync(id);
if (entity == null)
{
    _logger.LogWarning(""Record with ID {Id} not found."", id);
    return NotFound(""Record not found."");
}
var value = entity.RelatedProperty?.Value ?? string.Empty;";
            return fix;
        }

        // 2. ArgumentNullException or ArgumentException
        if (ex is ArgumentNullException || ex is ArgumentException)
        {
            var paramName = (ex as ArgumentNullException)?.ParamName ?? (ex as ArgumentException)?.ParamName ?? "unknown";
            fix.RootCauseSummary = $"Invalid or null argument '{paramName}' was passed to {location.MethodName}() {locDesc}. Message: '{msg}'.";
            fix.RecommendedAction = $"Validate caller parameters before calling {location.MethodName}(). Ensure '{paramName}' is supplied and not empty.";
            fix.DiagnosticSteps = new List<string>
            {
                $"Inspect calling method that invokes {location.MethodName}().",
                $"Verify why '{paramName}' was null or contained invalid data.",
                "Add parameter validation attributes (e.g. [Required]) or caller validation: 'ArgumentNullException.ThrowIfNull(param);'."
            };
            fix.SuggestedCodePattern = @"// Example Fix: Guard against null arguments
ArgumentNullException.ThrowIfNull(request, nameof(request));
if (string.IsNullOrWhiteSpace(request.Code))
    throw new ArgumentException(""Code is required."", nameof(request));";
            return fix;
        }

        // 3. Database & SQL Exceptions
        if (fullTypeName.Contains("SqlException", StringComparison.OrdinalIgnoreCase) ||
            fullTypeName.Contains("DbUpdate", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Cannot insert duplicate key", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("deadlock victim", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
        {
            return AnalyzeDatabaseException(ex, location, locDesc, msg);
        }

        // 4. Concurrency Exceptions
        if (fullTypeName.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
        {
            fix.RootCauseSummary = $"Optimistic concurrency conflict occurred {locDesc}. Another user or background task updated or deleted the record after it was loaded into memory.";
            fix.RecommendedAction = "Reload the entity from the database or handle DbUpdateConcurrencyException by prompting the user or retrying with latest data.";
            fix.DiagnosticSteps = new List<string>
            {
                "Inspect the concurrency token / rowversion on the modified entity.",
                "Catch DbUpdateConcurrencyException in the service method and call 'entry.ReloadAsync()'.",
                "Inform the user that the record was modified by another transaction and allow refreshing the form."
            };
            fix.SuggestedCodePattern = @"try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    foreach (var entry in ex.Entries)
    {
        await entry.ReloadAsync();
    }
    throw new ConcurrencyException(""The record was modified by another user. Please refresh and try again."");
}";
            return fix;
        }

        // 5. InvalidOperationException
        if (ex is InvalidOperationException || exType.Contains("InvalidOperation", StringComparison.OrdinalIgnoreCase))
        {
            if (msg.Contains("Sequence contains no elements", StringComparison.OrdinalIgnoreCase))
            {
                fix.RootCauseSummary = $"A LINQ query called .First() or .Single() on an empty sequence {locDesc}.";
                fix.RecommendedAction = "Change .First() to .FirstOrDefault() or .Single() to .SingleOrDefault(), and handle the null outcome safely.";
                fix.DiagnosticSteps = new List<string>
                {
                    $"Inspect line {location.LineNumber} in {location.FileName}.",
                    "Locate LINQ operations using .First(), .Single(), or .Max() on an unverified list.",
                    "Replace with .FirstOrDefault() and add a null check."
                };
                fix.SuggestedCodePattern = @"var item = items.FirstOrDefault(x => x.Id == targetId);
if (item == null)
{
    // Handle missing item cleanly
    return;
}";
                return fix;
            }

            if (msg.Contains("Collection was modified", StringComparison.OrdinalIgnoreCase))
            {
                fix.RootCauseSummary = $"A collection was modified while being enumerated in a foreach loop {locDesc}.";
                fix.RecommendedAction = "Create a snapshot using .ToList() before iterating, or use a traditional for-loop or thread-safe collection.";
                fix.DiagnosticSteps = new List<string>
                {
                    $"Check the foreach loop in {location.FileName} at line {location.LineNumber}.",
                    "Ensure items are not added or removed from the enumerated collection inside the loop.",
                    "Use 'foreach (var item in collection.ToList())' to iterate over a stable copy."
                };
                fix.SuggestedCodePattern = @"// Iterate over a cloned snapshot:
foreach (var item in activeQueue.ToList())
{
    activeQueue.Remove(item);
}";
                return fix;
            }

            if (msg.Contains("second operation was started on this context", StringComparison.OrdinalIgnoreCase))
            {
                fix.RootCauseSummary = $"Concurrent access detected on a single DbContext instance {locDesc}. Entity Framework Core DbContext is not thread-safe.";
                fix.RecommendedAction = "Ensure DbContext is not shared across async tasks or threads. Use IDbContextFactory<TContext> or separate scopes.";
                fix.DiagnosticSteps = new List<string>
                {
                    "Check if multiple Task.Run or parallel tasks share the same scoped DbContext.",
                    "Inject IDbContextFactory<HrmTeContext> and create a dedicated context per thread/task using 'using var context = factory.CreateDbContext()'."
                };
                fix.SuggestedCodePattern = @"// Use IDbContextFactory for background or parallel threads:
await using var context = await _contextFactory.CreateDbContextAsync();
var result = await context.Set<Employee>().ToListAsync();";
                return fix;
            }

            fix.RootCauseSummary = $"Invalid operation occurred {locDesc}: '{msg}'. The application state was unsuitable for this operation.";
            fix.RecommendedAction = "Inspect state preconditions before triggering the operation.";
            fix.DiagnosticSteps = new List<string>
            {
                $"Inspect {location.FileName} around line {location.LineNumber}.",
                "Verify state transitions and prerequisites before invocation."
            };
            return fix;
        }

        // 6. HTTP & Remote Connection Errors
        if (ex is HttpRequestException || fullTypeName.Contains("Socket", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) || msg.Contains("forcibly closed", StringComparison.OrdinalIgnoreCase))
        {
            fix.RootCauseSummary = $"Outbound HTTP or TCP connection failed {locDesc}. The target service was unreachable or refused connection. Message: '{msg}'.";
            fix.RecommendedAction = "Check if target service (e.g. HRM.Api, identity provider, or microservice) is running, verify base URL, port, and network proxy.";
            fix.DiagnosticSteps = new List<string>
            {
                "Verify the remote endpoint URL in appsettings.json or environment variables.",
                "Check if HRM.Api is running and listening on the configured port.",
                "Inspect proxy, TLS certificates, and firewall policies.",
                "Review HttpClient retry policy or timeout configuration."
            };
            fix.SuggestedCodePattern = @"// Check HttpClient configuration:
builder.Services.AddHttpClient(""backend"", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});";
            return fix;
        }

        // 7. Security & Authorization
        if (ex is UnauthorizedAccessException || fullTypeName.Contains("Security", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Forbidden", StringComparison.OrdinalIgnoreCase) || msg.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            fix.RootCauseSummary = $"Authorization or access check failure occurred {locDesc}. The user or process lacks required permissions. Message: '{msg}'.";
            fix.RecommendedAction = "Check user roles, eFaas claims, and security access grants for this resource.";
            fix.DiagnosticSteps = new List<string>
            {
                "Check whether user is authenticated and token has not expired.",
                "Inspect required roles / permissions for this feature in UserContext or [Authorize] attributes.",
                "Verify file system permissions if a physical file or directory was being accessed."
            };
            return fix;
        }

        // 8. KeyNotFoundException
        if (ex is KeyNotFoundException)
        {
            fix.RootCauseSummary = $"A requested dictionary key or configuration entry was not found {locDesc}. Message: '{msg}'.";
            fix.RecommendedAction = "Use .TryGetValue() or .ContainsKey() before accessing dictionaries, or verify configuration keys.";
            fix.DiagnosticSteps = new List<string>
            {
                $"Navigate to line {location.LineNumber} in {location.FileName}.",
                "Identify the dictionary or map key being looked up.",
                "Use dict.TryGetValue(key, out var val) to handle missing keys gracefully."
            };
            fix.SuggestedCodePattern = @"if (dictionary.TryGetValue(lookupKey, out var item))
{
    // Use item
}
else
{
    // Handle missing key
}";
            return fix;
        }

        // 9. TaskCanceledException / OperationCanceledException / Timeout
        if (ex is TaskCanceledException || ex is OperationCanceledException || ex is TimeoutException)
        {
            fix.RootCauseSummary = $"An asynchronous operation timed out or was canceled via CancellationToken {locDesc}.";
            fix.RecommendedAction = "Check whether the query or operation is taking longer than expected, or if client disconnected.";
            fix.DiagnosticSteps = new List<string>
            {
                "If occurring during HTTP requests, check if client browser closed the tab or aborted navigation.",
                "Inspect database query execution plans for slow queries or missing indexes.",
                "Increase timeout threshold if processing large payroll or attendance batches."
            };
            return fix;
        }

        // 10. General / Fallback
        fix.RootCauseSummary = $"Unhandled {exType} thrown {locDesc}: '{msg}'.";
        fix.RecommendedAction = $"Inspect {location.FileName} around line {location.LineNumber} to determine root cause and implement targeted error handling.";
        fix.DiagnosticSteps = new List<string>
        {
            $"Open {location.FileName} at line {location.LineNumber}.",
            "Review call parameters, null safety, and database interactions.",
            "Inspect inner exception details if present for additional low-level clues."
        };

        return fix;
    }

    private static FixRecommendation AnalyzeDatabaseException(Exception ex, ErrorLocation location, string locDesc, string msg)
    {
        var fix = new FixRecommendation();

        // Unique constraint violation (SQL Error 2601 / 2627)
        if (msg.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Cannot insert duplicate key", StringComparison.OrdinalIgnoreCase))
        {
            fix.RootCauseSummary = $"Database unique key violation occurred {locDesc}. An attempt was made to insert or update a record with a value that already exists in a unique column/index.";
            fix.RecommendedAction = "Ensure business keys (e.g. employee number, code, unique combination) are validated for uniqueness before persisting.";
            fix.DiagnosticSteps = new List<string>
            {
                "Identify which unique index or constraint was violated in the SQL message.",
                "Query the database table to inspect the existing conflicting record.",
                "Add an existence check prior to saving: 'if (await _context.Set.AnyAsync(x => x.Code == code)) return Conflict();'."
            };
            fix.SuggestedCodePattern = @"var exists = await _context.Staff.AnyAsync(s => s.EmployeeNumber == empNumber);
if (exists)
{
    throw new ValidationException(""An employee with this Employee Number already exists."");
}";
            return fix;
        }

        // Foreign key violation (SQL Error 547)
        if (msg.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase))
        {
            fix.RootCauseSummary = $"Database foreign key violation occurred {locDesc}. The referenced parent entity does not exist, or dependent records prevent deletion.";
            fix.RecommendedAction = "Verify referenced primary key IDs exist before inserting, and use soft-deletion or cascade rules for parent deletions.";
            fix.DiagnosticSteps = new List<string>
            {
                "Inspect the foreign key name in the error message.",
                "Ensure parent record ID exists in the referenced table before saving child entity.",
                "Verify whether you are deleting a record that has active dependencies."
            };
            return fix;
        }

        // Deadlock (SQL Error 1205)
        if (msg.Contains("deadlock victim", StringComparison.OrdinalIgnoreCase) || msg.Contains("1205", StringComparison.OrdinalIgnoreCase))
        {
            fix.RootCauseSummary = $"Database transaction deadlock occurred {locDesc}. Two concurrent operations locked resources in reverse order.";
            fix.RecommendedAction = "Enable SQL retry on failure and ensure transactions access tables in a consistent order.";
            fix.DiagnosticSteps = new List<string>
            {
                "Configure SQL retry on failure: options.UseSqlServer(conn, sql => sql.EnableRetryOnFailure()).",
                "Ensure transactions update tables in the same consistent sequence.",
                "Keep transactions as short as possible."
            };
            fix.SuggestedCodePattern = @"services.AddDbContext<HrmTeContext>(opt =>
    opt.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));";
            return fix;
        }

        // Timeout (SQL Error -2)
        if (msg.Contains("Timeout expired", StringComparison.OrdinalIgnoreCase) || msg.Contains("Execution Timeout Expired", StringComparison.OrdinalIgnoreCase))
        {
            fix.RootCauseSummary = $"Database command execution timed out {locDesc}. The query took longer than the configured CommandTimeout.";
            fix.RecommendedAction = "Optimize SQL query execution plan, add missing table indexes, or increase CommandTimeout for bulk operations.";
            fix.DiagnosticSteps = new List<string>
            {
                "Inspect the SQL query generated by Entity Framework.",
                "Check table indexes for missing indexes on filter/join columns.",
                "For heavy payroll or attendance batches, set context.Database.SetCommandTimeout(120)."
            };
            return fix;
        }

        // Invalid object name (Missing table or column - SQL Error 208)
        if (msg.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
        {
            fix.RootCauseSummary = $"Database table or view not found {locDesc}. A referenced physical table does not exist in the database.";
            fix.RecommendedAction = "Ensure pending database migrations or embedded SQL scripts have been applied to this database instance.";
            fix.DiagnosticSteps = new List<string>
            {
                "Check the missing table name in the error message.",
                "Verify if the table adheres to prefix naming convention (e.g. ERR_, PAY_, AR_).",
                "Run pending database migration scripts or check DatabaseBaseline."
            };
            return fix;
        }

        fix.RootCauseSummary = $"Database error occurred {locDesc}: '{msg}'.";
        fix.RecommendedAction = "Inspect database connection string, schema, and Entity Framework model mapping.";
        fix.DiagnosticSteps = new List<string>
        {
            "Verify database server connectivity and credentials.",
            "Check inner exception for SQL Server error code and details."
        };
        return fix;
    }
}
