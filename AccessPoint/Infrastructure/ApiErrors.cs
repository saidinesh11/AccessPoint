using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace App.Infrastructure;
public sealed class WorkflowException(int status, string message) : Exception(message) { public int Status { get; } = status; }
public sealed class ApiErrors(RequestDelegate next, ILogger<ApiErrors> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try { await next(context); }
        catch (Exception error)
        {
            if (context.Response.HasStarted) throw;
            var (status, detail) = error switch
            {
                WorkflowException workflow => (workflow.Status, workflow.Message),
                DbUpdateConcurrencyException => (409, "This record changed. Refresh it before submitting again."),
                DbUpdateException update when IsUniqueConflict(update) => (409, "A competing operation already reserved or created this record. Refresh and retry."),
                _ => (500, "The operation could not be completed. Use the trace identifier when reporting the error.")
            };
            if (status == 500) logger.LogError(error, "Unhandled API failure {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = status;
            var problem = new ProblemDetails { Status = status, Title = status == 409 ? "Conflict" : "Request failed", Detail = detail };
            problem.Extensions["traceId"] = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
    private static bool IsUniqueConflict(DbUpdateException error) => error.InnerException switch
    {
        Microsoft.Data.Sqlite.SqliteException sqlite => sqlite.SqliteExtendedErrorCode is 2067 or 1555,
        Microsoft.Data.SqlClient.SqlException sql => sql.Number is 2601 or 2627,
        _ => false
    };
}
