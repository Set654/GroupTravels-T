using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripService.Domain.Entities;


namespace TripService.Infrastructure.Persistence.Configuratinos;

    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.ToTable("expenses", table =>
            {
                table.HasCheckConstraint("CK_expenses_amount", "amount>0");
            });

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("expense_id").ValueGeneratedOnAdd();

            builder.Property(e => e.TripId).HasColumnName("trip_id").IsRequired();

            builder.Property(e => e.PaidMemberId).HasColumnName("paid_by_member_id").IsRequired();

            builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(500).IsRequired();

            builder.Property(e => e.Category).HasColumnName("category").HasMaxLength(100).IsRequired();

            builder.Property(e => e.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)").IsRequired();

            builder.Property(e => e.ReceiptPhotoUrl).HasColumnName("receipt_photo_url").HasColumnType("text").IsRequired(false);
            builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();

            builder.HasOne(e => e.Trip).WithMany().HasForeignKey(e => e.TripId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(e => e.PaidByMember).WithMany().HasForeignKey(e => e.PaidMemberId).OnDelete(DeleteBehavior.Restrict);



        }
    }
