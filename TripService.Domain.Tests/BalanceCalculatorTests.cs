namespace TripService.Domain.Tests
{
    public class BalanceCalculatorTests
    {
        [Fact]
        public void CalculateBalance_WhenPaidExceedsShare_ReturnsPositiveBalance()
        {
            decimal paid = 1000m;
            decimal share = 350m;

            decimal balance = BalanceCalculator.CalculateBalance(paid,share);

            Assert.Equal(650m, balance);
        }
    }
}
