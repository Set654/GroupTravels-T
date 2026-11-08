
namespace TripService.Domain.Entities;

    public class TripMember
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int TripId { get; set; }
        public string? VirtualName { get; set; }
        public required string Role { get; set; }
        public required string Status { get; set; }
        public User? User { get; set; }
        public Trip Trip { get; set; } = null!;

    }

