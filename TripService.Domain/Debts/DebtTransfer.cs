

namespace TripService.Domain.Debts
{
    public sealed record DebtTransfer(int DebtorParticipantId, int CreditorParticipantId, decimal Amount);
}
