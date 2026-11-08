using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripService.Domain.Entities;


namespace TripService.Infrastructure.Persistence.Configuratinos;

    public class TripConfiguration : IEntityTypeConfiguration<Trip>
    {
        public void Configure(EntityTypeBuilder<Trip> builder)

        {

            builder.ToTable("trips", table => { table.HasCheckConstraint("CK_trips_dates", "end_date >= start_date"); });
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("trip_id").ValueGeneratedOnAdd();

            builder.Property(t => t.Name).HasColumnName("trip_name").HasMaxLength(100).IsRequired();

            builder.Property(t => t.StartDate).HasColumnName("start_date").HasColumnType("date").IsRequired();

            builder.Property(t => t.EndDate).HasColumnName("end_date").HasColumnType("date").IsRequired();

            builder.Property(t => t.CoverUrl).HasColumnName("cover_url").HasColumnType("text").IsRequired(false);
        }
    }
