using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripService.Domain.Entities;


namespace TripService.Infrastructure.Persistence.Configuratinos;

    public class LogConfiguration : IEntityTypeConfiguration<Log>
    {
        public void Configure(EntityTypeBuilder<Log> builder)
        {
            builder.ToTable("logs", table =>
            {
                table.HasCheckConstraint("CK_logs_debt_settled", @"action_type <> 'DebtSettled' OR (from_member_id IS NOT NULL
                    AND to_member_id IS NOT NULL AND amount>0 AND from_member_id <> to_member_id)");
            });
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Id).HasColumnName("log_id").ValueGeneratedOnAdd();
            builder.Property(l => l.UserId).HasColumnName("user_id").IsRequired(false);
            builder.Property(l => l.TripId).HasColumnName("trip_id").IsRequired();

            builder.Property(l => l.ActionMessage).HasColumnName("action_message").HasColumnType("text").IsRequired();

            builder.Property(l => l.ActionType).HasColumnName("action_type").HasMaxLength(100).IsRequired();

            builder.Property(l => l.FromMemberId).HasColumnName("from_member_id").IsRequired(false);
            builder.Property(l => l.ToMemberId).HasColumnName("to_member_id").IsRequired(false);

            builder.Property(l => l.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)").IsRequired(false);

            builder.Property(l => l.Timestamp).HasColumnName("timestamp").HasColumnType("timestamp with time zone").IsRequired();

            builder.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(l => l.Trip).WithMany().HasForeignKey(l => l.TripId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(l => l.FromMember).WithMany().HasForeignKey(l => l.FromMemberId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(l => l.ToMember).WithMany().HasForeignKey(l => l.ToMemberId).OnDelete(DeleteBehavior.Restrict);




        }
    }
