using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using App.Domain;
namespace App.Data;
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<AuditEvent> Audit => Set<AuditEvent>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<DeliveryRecord> Deliveries => Set<DeliveryRecord>();
    public DbSet<CommandReceipt> Receipts => Set<CommandReceipt>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<ScheduleSlot> Slots => Set<ScheduleSlot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<AuditEvent>().HasIndex(a=>new {a.EntityType,a.EntityId,a.Id});
        model.Entity<AuditEvent>().Property(a=>a.EntityType).HasMaxLength(60);
        model.Entity<AuditEvent>().Property(a=>a.Detail).HasMaxLength(2000);
        model.Entity<CommandReceipt>().Property(r=>r.ActorId).HasMaxLength(450);
        model.Entity<CommandReceipt>().Property(r=>r.Key).HasMaxLength(36);
        model.Entity<CommandReceipt>().HasIndex(r=>new {r.ActorId,r.Key}).IsUnique();
        model.Entity<OutboxMessage>().HasIndex(m=>new {m.ProcessedUtc,m.CreatedUtc});
        model.Entity<Provider>().Property(p=>p.Name).HasMaxLength(120);
        model.Entity<ScheduleSlot>().HasIndex(s=>new {s.ProviderId,s.StartUtc}).IsUnique();
        model.Entity<ScheduleSlot>().HasOne(s=>s.Provider).WithMany().HasForeignKey(s=>s.ProviderId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Booking>().HasOne(b=>b.Slot).WithMany().HasForeignKey(b=>b.SlotId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Booking>().Property(b=>b.Status).HasMaxLength(30);
        model.Entity<Booking>().Property(b=>b.OwnerId).HasMaxLength(450);
        model.Entity<Booking>().Property(b=>b.Version).IsConcurrencyToken();
        model.Entity<Booking>().HasIndex(b=>b.SlotId).IsUnique().HasFilter("[Status] <> 'Cancelled'");
        model.Entity<Booking>().HasIndex(b=>new {b.OwnerId,b.CreatedUtc});
        var utc = new ValueConverter<DateTime,DateTime>(v=>v,v=>DateTime.SpecifyKind(v,DateTimeKind.Utc));
        foreach(var entity in model.Model.GetEntityTypes())
            foreach(var property in entity.GetProperties())
                if(property.ClrType==typeof(DateTime)||property.ClrType==typeof(DateTime?))property.SetValueConverter(utc);
    }
}
