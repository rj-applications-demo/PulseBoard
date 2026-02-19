using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PulseBoard.Domain;

namespace PulseBoard.Infrastructure.Entities.Configurations;

public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");

        builder.HasKey(k => k.Id);

        builder.HasIndex(k => k.TenantId);

        builder.HasIndex(k => k.KeyHash)
            .IsUnique();

        builder.Property(k => k.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(k => k.KeyHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(k => k.Tier)
            .HasConversion<int>()
            .HasDefaultValue(ApiKeyTier.Free)
            .IsRequired();

        builder.Property(k => k.CreatedUtc)
            .IsRequired();
    }
}
