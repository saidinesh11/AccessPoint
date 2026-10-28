using Microsoft.AspNetCore.Identity;
namespace App.Domain;
public sealed class AppUser : IdentityUser { public string DisplayName { get; set; } = ""; }
public sealed class AuditEvent
{
    public long Id { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string ActorId { get; set; } = "";
    public string Action { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
}
public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Kind { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public DateTime? ProcessedUtc { get; set; }
}
public sealed class DeliveryRecord
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = "";
    public DateTime DeliveredUtc { get; set; }
}
public sealed class CommandReceipt
{
    public long Id { get; set; }
    public string ActorId { get; set; } = "";
    public string Key { get; set; } = "";
    public string Fingerprint { get; set; } = "";
    public Guid ResultId { get; set; }
}
