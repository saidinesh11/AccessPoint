using Microsoft.EntityFrameworkCore;
using App.Data;
using App.Domain;
using App.Infrastructure;
namespace App.Services;
public sealed class WorkflowService(AppDbContext db, CurrentActor actor, TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    public IQueryable<Booking> VisibleBookings() => actor.IsStaff ? db.Bookings : db.Bookings.Where(b => b.OwnerId == actor.Id);
    public async Task<Booking> Get(Guid id, CancellationToken ct)
    {
        var value = await VisibleBookings().Include(b => b.Slot).ThenInclude(s => s.Provider).SingleOrDefaultAsync(b => b.Id == id, ct);
        return value ?? throw new WorkflowException(404, "Booking not found.");
    }
    public async Task<(Booking Booking, bool Replayed)> Reserve(ReserveInput input, string key, CancellationToken ct)
    {
        if (!Guid.TryParse(key, out _)) throw new WorkflowException(400, "Send a UUID in the Idempotency-Key header.");
        if (!BookingRules.Services.Contains(input.Service) || input.Format is not "Video" and not "Phone")
            throw new WorkflowException(400, "Choose a supported service and meeting format.");
        var fingerprint = WorkflowWrites.Fingerprint(input);
        var receipt = await db.Receipts.SingleOrDefaultAsync(r => r.ActorId == actor.Id && r.Key == key, ct);
        if (receipt is not null)
        {
            if (receipt.Fingerprint != fingerprint) throw new WorkflowException(409, "This idempotency key was used for different details.");
            return (await Get(receipt.ResultId, ct), true);
        }
        var slot = await GetAvailableSlot(input.SlotId, null, ct);
        var booking = new Booking { SlotId = slot.Id, OwnerId = actor.Id, Service = input.Service, Format = input.Format, CreatedUtc = Now, Slot = slot };
        db.Bookings.Add(booking);
        db.Receipts.Add(new CommandReceipt { ActorId = actor.Id, Key = key, Fingerprint = fingerprint, ResultId = booking.Id });
        db.Record("Booking", booking.Id, actor.Id, "Created", input.Service, Now);
        // EF commits booking, command receipt, audit and outbox in one transaction.
        // The filtered unique index on SlotId is the final authority under competing writers.
        await db.SaveChangesAsync(ct);
        return (booking, false);
    }
    public async Task<Booking> Reschedule(Guid id, RescheduleInput input, CancellationToken ct)
    {
        var booking = await Get(id, ct);
        RequireVersion(booking, input.Version);
        if (booking.Status != "Reserved" || booking.Slot.StartUtc <= Now) throw new WorkflowException(409, "Only future reserved appointments can be rescheduled.");
        var slot = await GetAvailableSlot(input.SlotId, id, ct);
        var previous = booking.SlotId;
        booking.SlotId = slot.Id; booking.Slot = slot; booking.Version = Guid.NewGuid();
        db.Record("Booking", id, actor.Id, "Rescheduled", $"Slot {previous} → {slot.Id}", Now);
        await db.SaveChangesAsync(ct);
        return booking;
    }
    public async Task<Booking> Transition(Guid id, ActionInput input, CancellationToken ct)
    {
        var booking = await Get(id, ct); RequireVersion(booking, input.Version);
        if (!BookingRules.CanTransition(booking.Status, input.Action, actor.IsStaff)) throw new WorkflowException(409, "That action is not permitted for this status or role.");
        if (!actor.IsStaff && booking.Slot.StartUtc <= Now) throw new WorkflowException(409, "Contact staff to change an appointment that has started.");
        var previous = booking.Status; booking.Status = input.Action; booking.Version = Guid.NewGuid();
        db.Record("Booking", id, actor.Id, "StatusChanged", $"{previous} → {booking.Status}", Now);
        await db.SaveChangesAsync(ct); return booking;
    }
    public async Task<ScheduleSlot> CreateSlot(SlotInput input, CancellationToken ct)
    {
        if (!actor.IsAdmin) throw new WorkflowException(403, "Administrator access required.");
        var start = DateTime.SpecifyKind(input.StartUtc, DateTimeKind.Utc);
        if (start <= Now || start > Now.AddDays(90) || start.Minute is not 0 and not 30 || start.Second != 0)
            throw new WorkflowException(400, "Choose a future half-hour boundary within 90 days, in UTC.");
        if (!await db.Providers.AnyAsync(p => p.Id == input.ProviderId, ct)) throw new WorkflowException(400, "Unknown provider.");
        var slot = new ScheduleSlot { ProviderId = input.ProviderId, StartUtc = start, EndUtc = start.AddMinutes(30) };
        db.Slots.Add(slot); db.Record("ScheduleSlot", slot.Id, actor.Id, "Created", start.ToString("O"), Now);
        await db.SaveChangesAsync(ct); return slot;
    }
    private async Task<ScheduleSlot> GetAvailableSlot(Guid id, Guid? excludeBooking, CancellationToken ct)
    {
        var slot = await db.Slots.Include(s => s.Provider).SingleOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new WorkflowException(404, "Slot not found.");
        if (slot.StartUtc <= Now) throw new WorkflowException(409, "This slot is in the past.");
        if (await db.Bookings.AnyAsync(b => b.SlotId == id && b.Status != "Cancelled" && b.Id != excludeBooking, ct))
            throw new WorkflowException(409, "This slot has already been reserved.");
        return slot;
    }
    private static void RequireVersion(Booking booking, Guid version)
    {
        if (booking.Version != version) throw new WorkflowException(409, "Booking changed. Refresh before continuing.");
    }
    public Task RunMaintenance(CancellationToken ct) => Task.CompletedTask;
}
