
namespace TripService.Application.Trips
{
    public class GetTripService
    {
        private readonly ITripRepository _tripRepository;
        public GetTripService(ITripRepository tripRepository)
        {
            _tripRepository = tripRepository;
        }

        public Task<TripInfo?> GetAsync(int userId, int tripId, DateOnly today, CancellationToken ct)
        {
            return _tripRepository.GetAsync(userId, tripId, today, ct);
        }
    }
}
