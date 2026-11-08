using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripService.Domain.Entities;

namespace TripService.Infrastructure.Persistence.Configuratinos;
    public class TripMemberConfiguration : IEntityTypeConfiguration<TripMember>
    {
        public void Configure(EntityTypeBuilder<TripMember> builder)
        {
            builder.ToTable("trip_members", table => {
                table.HasCheckConstraint("CK_trip_members_identity",
        """
        (user_id IS NOT NULL AND virtual_name IS NULL)
        OR
        (user_id IS NULL AND virtual_name IS NOT NULL)
        """);
            });


            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).HasColumnName("member_id").ValueGeneratedOnAdd();

            builder.Property(m => m.UserId).HasColumnName("user_id").IsRequired(false);

            builder.Property(m => m.TripId).HasColumnName("trip_id").IsRequired();

            builder.Property(m => m.VirtualName).HasColumnName("virtual_name").HasMaxLength(100).IsRequired(false);

            builder.Property(m => m.Role).HasColumnName("role").HasMaxLength(20).IsRequired();

            builder.Property(m => m.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            builder.HasIndex(m => new { m.TripId, m.UserId }).IsUnique();

            builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Trip).WithMany().HasForeignKey(m => m.TripId).OnDelete(DeleteBehavior.Restrict);
        }
    }
