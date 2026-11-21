using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TripService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsOtpStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sms_otp_challenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CodeHmac = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sms_otp_challenges", x => x.Id);
                    table.CheckConstraint("CK_sms_otp_challenges_active_dates", "\"Status\" <> 'Active' OR (\"ActivatedAtUtc\" IS NOT NULL AND \"ExpiresAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_sms_otp_challenges_attempt_count", "\"AttemptCount\" >= 0 AND \"AttemptCount\" <= 5");
                    table.CheckConstraint("CK_sms_otp_challenges_dates", "\"ExpiresAtUtc\" IS NULL OR (\"ActivatedAtUtc\" IS NOT NULL AND \"ExpiresAtUtc\" > \"ActivatedAtUtc\")");
                    table.CheckConstraint("CK_sms_otp_challenges_hmac_length", "octet_length(\"CodeHmac\") = 32");
                    table.CheckConstraint("CK_sms_otp_challenges_status", "\"Status\" IN ('Pending', 'Active', 'Consumed', 'Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "sms_otp_send_attempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sms_otp_send_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sms_otp_send_attempts_sms_otp_challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "sms_otp_challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sms_otp_challenges_phone_pending",
                table: "sms_otp_challenges",
                column: "Phone",
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_sms_otp_send_attempts_ChallengeId",
                table: "sms_otp_send_attempts",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_sms_otp_send_attempts_IpAddress_RequestedAtUtc",
                table: "sms_otp_send_attempts",
                columns: new[] { "IpAddress", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_otp_send_attempts_Phone_RequestedAtUtc",
                table: "sms_otp_send_attempts",
                columns: new[] { "Phone", "RequestedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sms_otp_send_attempts");

            migrationBuilder.DropTable(
                name: "sms_otp_challenges");
        }
    }
}
