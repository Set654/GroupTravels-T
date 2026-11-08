using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripService.Domain.Entities;

namespace TripService.Infrastructure.Persistence.Configuratinos;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("user_id").ValueGeneratedOnAdd();

        builder.Property(u => u.Name).HasColumnName("username").HasMaxLength(100).IsRequired();

        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(150).IsRequired(false);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Phone).HasColumnName("phone").HasMaxLength(20).IsRequired();
        builder.HasIndex(u => u.Phone).IsUnique();

        builder.Property(u => u.TbankPaymentUrl).HasColumnName("tbank_payment_url").HasColumnType("text").IsRequired(false);

        builder.Property(u => u.IsDarkTheme).HasColumnName("is_dark_theme").HasDefaultValue(false).IsRequired();
    }
}