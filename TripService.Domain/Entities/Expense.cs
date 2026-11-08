
namespace TripService.Domain.Entities;

    public class Expense
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public int PaidMemberId { get; set; }
        public required string Title { get; set; }
        public required string Category { get; set; }
        public decimal Amount { get; set; }
        public string? ReceiptPhotoUrl { get; set; }
        public DateTime CreatedAt { get; set; }


        public Trip Trip { get; set; } = null!;
        public TripMember PaidByMember { get; set; } = null!;
    }
