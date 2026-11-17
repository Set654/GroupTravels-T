
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripService.Domain.Entities;

namespace TripService.Infrastructure.Persistence.Configurations;

public class SmsOtpChallengeConfiguration
    : IEntityTypeConfiguration<SmsOtpChallenge>
{
    public void Configure(EntityTypeBuilder<SmsOtpChallenge> builder)
    {
        builder.ToTable("sms_otp_challenges", table =>
        {
            table.HasCheckConstraint(
                "CK_sms_otp_challenges_status",
                "\"Status\" IN ('Pending', 'Active', 'Consumed', 'Cancelled')");

            table.HasCheckConstraint(
                "CK_sms_otp_challenges_attempt_count",
                "\"AttemptCount\" >= 0 AND \"AttemptCount\" <= 5");

            table.HasCheckConstraint(
                "CK_sms_otp_challenges_active_dates",
                "\"Status\" <> 'Active' OR " +
                "(\"ActivatedAtUtc\" IS NOT NULL AND \"ExpiresAtUtc\" IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_sms_otp_challenges_hmac_length",
                "octet_length(\"CodeHmac\") = 32");
            table.HasCheckConstraint(
                "CK_sms_otp_challenges_dates",
                "\"ExpiresAtUtc\" IS NULL OR " +
                "(\"ActivatedAtUtc\" IS NOT NULL AND " +
                "\"ExpiresAtUtc\" > \"ActivatedAtUtc\")");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Phone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CodeHmac)
            .HasColumnType("bytea")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ActivatedAtUtc);

        builder.Property(x => x.ExpiresAtUtc);

        builder.Property(x => x.AttemptCount)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.LastSentAtUtc);

        builder.Property(x => x.ConsumedAtUtc);

        builder.HasIndex(x => x.Phone).IsUnique().HasDatabaseName("IX_sms_otp_challenges_phone_pending").HasFilter("\"Status\" = 'Pending'");
    }
}
