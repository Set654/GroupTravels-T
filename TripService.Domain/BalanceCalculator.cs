
namespace TripService.Domain
{
    public static class BalanceCalculator
    {
        public static decimal CalculateBalance(decimal paid, decimal  share )
        {
            return paid - share;
        }
    }
}
