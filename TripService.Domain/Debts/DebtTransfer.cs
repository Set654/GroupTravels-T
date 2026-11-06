

namespace TripService.Domain.Debts
{
    public sealed record DebtTransfer(Guid DebtorParticipantId, Guid CreditorParticipantId, decimal Amount);
}
