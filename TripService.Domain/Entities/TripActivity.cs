
namespace TripService.Domain.Entities;
    public class TripActivity
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public required string Title { get; set; }
        public required string Type { get; set; }
        public string? Description { get; set; }
        public required string Location { get; set; }
        public string? SourceUrl { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public Trip Trip { get; set; } = null!;

    }

