using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Data;
using App.Domain;
using App.Infrastructure;
using App.Services;
namespace App.Controllers;
[ApiController, Authorize, Route("api")]
public sealed class BookingsController(AppDbContext db, WorkflowService workflow) : ControllerBase
{
    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken ct) => Ok(new { providers = await db.Providers.AsNoTracking().ToListAsync(ct), services = BookingRules.Services });
    [HttpGet("slots")]
    public async Task<IActionResult> Slots(Guid? providerId, DateTime? date, CancellationToken ct)
    {
        var from = (date ?? DateTime.UtcNow.AddDays(1)).Date;
        var until = from.AddDays(1);
        var query = db.Slots.AsNoTracking().Include(s => s.Provider).Where(s => s.StartUtc >= from && s.StartUtc < until && s.StartUtc > DateTime.UtcNow);
        if (providerId is not null) query = query.Where(s => s.ProviderId == providerId);
        var available = await query.Where(s => !db.Bookings.Any(b => b.SlotId == s.Id && b.Status != "Cancelled"))
            .OrderBy(s => s.StartUtc).Select(s => new { s.Id, s.ProviderId, provider = s.Provider.Name, s.StartUtc, s.EndUtc }).ToListAsync(ct);
        return Ok(available);
    }
    [HttpGet("bookings")]
    public async Task<IActionResult> List(string? status, int page = 1, CancellationToken ct = default)
    {
        page = Math.Clamp(page, 1, 10000);
        var query = workflow.VisibleBookings().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(b => b.Status == status);
        return Ok(new { total = await query.CountAsync(ct), page, pageSize = 10,
            items = await query.Include(b => b.Slot).ThenInclude(s => s.Provider).OrderByDescending(b => b.CreatedUtc)
                .Skip((page-1)*10).Take(10).ToListAsync(ct) });
    }
    [HttpGet("bookings/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await workflow.Get(id, ct));
    [HttpPost("bookings")]
    public async Task<IActionResult> Reserve(ReserveInput input, [FromHeader(Name = "Idempotency-Key")] string key, CancellationToken ct)
    {
        var result = await workflow.Reserve(input, key, ct);
        return result.Replayed ? Ok(result.Booking) : Created($"/api/bookings/{result.Booking.Id}", result.Booking);
    }
    [HttpPost("bookings/{id:guid}/reschedule")]
    public async Task<IActionResult> Reschedule(Guid id, RescheduleInput input, CancellationToken ct) => Ok(await workflow.Reschedule(id,input,ct));
    [HttpPost("bookings/{id:guid}/actions")]
    public async Task<IActionResult> Act(Guid id, ActionInput input, CancellationToken ct) => Ok(await workflow.Transition(id,input,ct));
    [HttpGet("bookings/{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, CancellationToken ct)
    {
        await workflow.Get(id,ct);
        return Ok(await db.Audit.AsNoTracking().Where(a => a.EntityType == "Booking" && a.EntityId == id).OrderBy(a => a.Id)
            .Select(a => new { a.Action,a.Detail,a.CreatedUtc }).ToListAsync(ct));
    }
    [HttpPost("slots"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSlot(SlotInput input, CancellationToken ct) => Ok(await workflow.CreateSlot(input,ct));
}
