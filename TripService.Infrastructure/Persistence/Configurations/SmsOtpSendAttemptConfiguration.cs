
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripService.Domain.Entities;

namespace TripService.Infrastructure.Persistence.Configurations;

public class SmsOtpSendAttemptConfiguration
    : IEntityTypeConfiguration<SmsOtpSendAttempt>
{
    public void Configure(EntityTypeBuilder<SmsOtpSendAttempt> builder)
    {
        builder.ToTable("sms_otp_send_attempts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Phone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(x => x.RequestedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Challenge)
            .WithMany()
            .HasForeignKey(x => x.ChallengeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new
        {
            x.Phone,
            x.RequestedAtUtc
        });

        builder.HasIndex(x => new
        {
            x.IpAddress,
            x.RequestedAtUtc
        });
    }
}
