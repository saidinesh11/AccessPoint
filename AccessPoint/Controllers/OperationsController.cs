using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Data;
namespace App.Controllers;
[ApiController, Authorize(Roles = "Admin"), Route("api/operations")]
public sealed class OperationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(new
    {
        pendingMessages = await db.Outbox.CountAsync(m => m.ProcessedUtc == null, ct),
        localDeliveries = await db.Deliveries.CountAsync(ct),
        recentAudit = await db.Audit.AsNoTracking().OrderByDescending(a => a.Id).Take(30).ToListAsync(ct)
    });
}
