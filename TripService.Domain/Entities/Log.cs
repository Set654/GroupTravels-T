

namespace TripService.Domain.Entities;
    public class Log
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int TripId { get; set; }
        public required string ActionMessage { get; set; }
        public required string ActionType { get; set; }
        public int? FromMemberId { get; set; }
        public int? ToMemberId { get; set; }
        public decimal? Amount { get; set; }
        public DateTime Timestamp { get; set; }

        public User? User { get; set; }
        public Trip Trip { get; set; } = null!;
        public TripMember? FromMember { get; set; }
        public TripMember? ToMember { get; set; }

    }
