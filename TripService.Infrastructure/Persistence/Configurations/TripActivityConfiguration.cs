using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripService.Domain.Entities;

namespace TripService.Infrastructure.Persistence.Configuratinos;

    public class TripActivityConfiguration : IEntityTypeConfiguration<TripActivity>
    {
        public void Configure(EntityTypeBuilder<TripActivity> builder)
        {
            builder.ToTable("trip_activity");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("activity_id").ValueGeneratedOnAdd();

            builder.Property(t => t.TripId).HasColumnName("trip_id").IsRequired();

            builder.Property(t => t.Title).HasColumnName("title").HasMaxLength(100).IsRequired();

            builder.Property(t => t.Type).HasColumnName("type").HasMaxLength(30).IsRequired();

            builder.Property(t => t.Description).HasColumnName("description").HasMaxLength(500).IsRequired(false);

            builder.Property(t => t.Location).HasColumnName("location").HasMaxLength(255).IsRequired();

            builder.Property(t => t.SourceUrl).HasColumnName("source_url").HasColumnType("text").IsRequired(false);

            builder.Property(t => t.ScheduledAt).HasColumnName("scheduled_at").HasColumnType("timestamp with time zone").IsRequired(false);

            builder.HasOne(t => t.Trip).WithMany().HasForeignKey(t => t.TripId).OnDelete(DeleteBehavior.Restrict);
        }

    }

