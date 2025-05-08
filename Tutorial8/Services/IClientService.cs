using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface IClientService
{
    Task<List<ClientTripDTO>> GetTripsForClient(int id);
    Task<int> CreateClient(CreateClientDTO client);
    Task<ServiceResult> RegisterClientForTrip(int clientId, int tripId);
    Task<bool> RemoveClientFromTrip(int clientId, int tripId);
}