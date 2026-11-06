
namespace TripService.Domain.Debts
{
     public static class DebtCalculator
    {
        private sealed class WorkingBalance
        {
            public Guid ParticipantId { get; }
            public decimal Remaining { get; set; }

            public WorkingBalance(Guid participantId, decimal balance)
            {
                ParticipantId = participantId;
                Remaining = balance;
            }
        }

        public static List<DebtTransfer> CalculateTransfers(IReadOnlyList<ParticipantBalance> participantBalances)
        {
            var creditors = participantBalances
                .Where(b => b.Balance > 0)
                .OrderByDescending(b => b.Balance)
                .Select(b => new WorkingBalance(
                    b.ParticipantId,
                    b.Balance))
                .ToList();

            var debtors = participantBalances
                .Where(b => b.Balance < 0)
                .OrderBy(b => b.Balance)
                .Select(b => new WorkingBalance(
                    b.ParticipantId,
                    b.Balance))
                .ToList();

            List<DebtTransfer> transfers = [];
            int creditorIndex = 0;
            int debtorIndex = 0;

            while (creditorIndex < creditors.Count && debtorIndex < debtors.Count)
            {
                var currCreditor = creditors[creditorIndex];
                var currDebtor = debtors[debtorIndex];
                decimal amountToSettle = Math.Min(currCreditor.Remaining, Math.Abs(currDebtor.Remaining));

                transfers.Add(new DebtTransfer(currDebtor.ParticipantId, currCreditor.ParticipantId, amountToSettle));

                currCreditor.Remaining -= amountToSettle;
                currDebtor.Remaining += amountToSettle;

                if (currCreditor.Remaining == 0) { creditorIndex++; }
                if (currDebtor.Remaining == 0) { debtorIndex++; }
            }
            return transfers;
        }
    }
}
