using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PulseBoard.Infrastructure.Entities.Configurations;

public sealed class AggregateBucketConfiguration : IEntityTypeConfiguration<AggregateBucket>
{
    public void Configure(EntityTypeBuilder<AggregateBucket> builder)
    {
        builder.ToTable("AggregateBuckets");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .UseIdentityColumn();

        // Unique index for upsert operations
        builder.HasIndex(a => new
        {
            a.TenantId,
            a.ProjectId,
            a.Metric,
            a.Interval,
            a.BucketStartUtc,
            a.DimensionKey
        })
        .IsUnique()
        .HasDatabaseName("IX_AggregateBuckets_Lookup");

        // Query index for time-series queries
        builder.HasIndex(a => new
        {
            a.TenantId,
            a.ProjectId,
            a.Metric,
            a.Interval,
            a.BucketStartUtc
        })
        .HasDatabaseName("IX_AggregateBuckets_TimeSeries");

        builder.Property(a => a.Metric)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Interval)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(a => a.DimensionKey)
            .HasMaxLength(200);

        builder.Property(a => a.BucketStartUtc)
            .IsRequired();

        builder.Property(a => a.Value)
            .IsRequired();

        builder.Property(a => a.UpdatedUtc)
            .IsRequired();

        builder.HasOne(a => a.Project)
            .WithMany()
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
