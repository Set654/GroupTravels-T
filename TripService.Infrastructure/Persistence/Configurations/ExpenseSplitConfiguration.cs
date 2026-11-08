using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripService.Domain.Entities;
namespace TripService.Infrastructure.Persistence.Configuratinos;

    public class ExpenseSplitConfiguration : IEntityTypeConfiguration<ExpenseSplit>
    {
        public void Configure(EntityTypeBuilder<ExpenseSplit> builder)
        {
            builder.ToTable("expense_splits", table => { table.HasCheckConstraint("CK_expense_splits_amount", "share_amount>=0"); });

            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).HasColumnName("split_id").ValueGeneratedOnAdd();

            builder.Property(s => s.ExpenseId).HasColumnName("expense_id").IsRequired();

            builder.Property(s => s.MemberId).HasColumnName("member_id").IsRequired();
            builder.Property(s => s.ShareAmount).HasColumnName("share_amount").HasColumnType("numeric(18,2)").IsRequired();

            builder.HasOne(s => s.Expense).WithMany().HasForeignKey(s => s.ExpenseId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(s => s.Member).WithMany().HasForeignKey(s => s.MemberId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(s => new { s.ExpenseId, s.MemberId }).IsUnique();

        }
    }
