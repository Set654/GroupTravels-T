

namespace TripService.Domain.Entities;
    public class ExpenseSplit
    {
        public int Id { get; set; }
        public int ExpenseId { get; set; }
        public int MemberId { get; set; }
        public decimal ShareAmount { get; set; }

        public Expense Expense { get; set; } = null!;
        public TripMember Member { get; set; } = null!;
    }

