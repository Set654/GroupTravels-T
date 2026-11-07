
using TripService.Domain.Debts;

namespace TripService.Domain.Tests
{
    public class DebtCalculatorTest
    {
        [Fact]
        public void CalculateTransfers_TwoOppositeBalances_ReturnsSingleTransfer()
        {
            int Id1 = 1;
            int Id2 = 2;
            decimal BalanceId1 = 700;
            decimal BalanceId2 = -700;

            var PB1 = new ParticipantBalance(Id1, BalanceId1);
            var PB2 = new ParticipantBalance(Id2, BalanceId2);
            List<ParticipantBalance> ListTr = [];
            ListTr.Add(PB1);
            ListTr.Add(PB2);
            var result = DebtCalculator.CalculateTransfers(ListTr);

            var transfer = Assert.Single(result);
            Assert.Equal(Id2, transfer.DebtorParticipantId);
            Assert.Equal(Id1, transfer.CreditorParticipantId);
            Assert.Equal(700m, transfer.Amount);
        }
    }
}
