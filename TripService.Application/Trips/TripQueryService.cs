namespace TripService.Application.Trips
{
    public class TripQueryService
    {
        private readonly ITripRepository _tripRepository;
        public TripQueryService(ITripRepository tripRepository)
        {
            _tripRepository = tripRepository;
        }

        public Task<TripInfo?> GetAsync(int userId, int tripId, DateOnly today, CancellationToken ct)
        {
            return _tripRepository.GetAsync(userId, tripId, today, ct);
        }

        public Task<IReadOnlyList<TripInfo>> ListAsync(int userId, DateOnly today, CancellationToken ct)
        {
            return _tripRepository.ListAsync(userId, today, ct);
        }
    }
}
