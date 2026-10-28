using System.ComponentModel.DataAnnotations;
namespace App.Domain;
public sealed class Provider
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Specialty { get; set; } = "";
}
public sealed class ScheduleSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
}
public sealed class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SlotId { get; set; }
    public ScheduleSlot Slot { get; set; } = null!;
    public string OwnerId { get; set; } = "";
    public string Service { get; set; } = "";
    public string Format { get; set; } = "Video";
    public string Status { get; set; } = "Reserved";
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime CreatedUtc { get; set; }
}
public sealed record ReserveInput(Guid SlotId, [Required, MaxLength(60)] string Service, [Required] string Format);
public sealed record RescheduleInput(Guid SlotId, Guid Version);
public sealed record ActionInput(Guid Version, [Required] string Action);
public sealed record SlotInput(Guid ProviderId, DateTime StartUtc);
public static class BookingRules
{
    public static readonly string[] Services = ["Application assistance", "Document review", "Accessibility consultation"];
    public static bool CanTransition(string from, string to, bool staff) =>
        to == "Cancelled" && from is "Reserved" or "CheckedIn"
        || staff && ((from == "Reserved" && to == "CheckedIn") || (from == "CheckedIn" && to == "Completed"));
}
