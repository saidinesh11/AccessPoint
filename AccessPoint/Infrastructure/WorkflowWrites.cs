using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using App.Data;
using App.Domain;
namespace App.Infrastructure;
public static class WorkflowWrites
{
    public static string Fingerprint<T>(T input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(input))));
    public static void Record(this AppDbContext db, string type, Guid id, string actor, string action, string detail, DateTime now)
    {
        db.Audit.Add(new AuditEvent { EntityType = type, EntityId = id, ActorId = actor, Action = action, Detail = detail, CreatedUtc = now });
        db.Outbox.Add(new OutboxMessage { Kind = type + "." + action, Payload = JsonSerializer.Serialize(new { id, actor }), CreatedUtc = now });
    }
}
