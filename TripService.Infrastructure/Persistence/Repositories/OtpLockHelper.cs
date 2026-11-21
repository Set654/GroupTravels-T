
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace TripService.Infrastructure.Persistence.Repositories;

internal static class OtpLockHelper
{
    public static async Task LockAsync(
        AppDbContext context,
        string key,
        CancellationToken cancellationToken = default)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(key));

        var lockId = BitConverter.ToInt64(hash, 0);

        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "Для OTP-блокировки требуется открытая транзакция.");

        var connection = context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.Transaction = transaction.GetDbTransaction();
        command.CommandText =
            "SELECT pg_advisory_xact_lock(@lockId)";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "lockId";
        parameter.Value = lockId;

        command.Parameters.Add(parameter);

        await command.ExecuteScalarAsync(cancellationToken);
    }
}
