using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PulseBoard.Infrastructure.Entities.Configurations;

public sealed class EventRecordConfiguration : IEntityTypeConfiguration<EventRecord>
{
    public void Configure(EntityTypeBuilder<EventRecord> builder)
    {
        builder.ToTable("EventRecords");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.EventId)
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.ProjectKey, e.TimestampUtc });

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.ProjectKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PayloadJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.TimestampUtc)
            .IsRequired();

        builder.Property(e => e.CreatedUtc)
            .IsRequired();

        builder.HasOne(e => e.Project)
            .WithMany(p => p.Events)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
