using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;
//Data retrieval from trip table
public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=apbd;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";    // Retrieve all trips for a specified client, along with registration and payment details

    
    // Retrieves all trips
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();
        var tripDict = new Dictionary<int, TripDTO>();
        // To get all rows from trip table and countries names for each of those trips
        string query = @"
            SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                   c.Name AS CountryName
            FROM Trip t
            LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
            LEFT JOIN Country c ON ct.IdCountry = c.IdCountry
            ORDER BY t.IdTrip";

        using (var conn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(query, conn))
        {
            await conn.OpenAsync();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int tripId = reader.GetInt32(0);

                    if (!tripDict.TryGetValue(tripId, out var trip))
                    {
                        trip = new TripDTO
                        {
                            Id = tripId,
                            Name = reader.GetString(1),
                            Description = reader.GetString(2),
                            DateFrom = reader.GetDateTime(3),
                            DateTo = reader.GetDateTime(4),
                            MaxPeople = reader.GetInt32(5),
                            Countries = new List<CountryDTO>()
                        };
                        tripDict[tripId] = trip;
                        trips.Add(trip);
                    }

                    if (!reader.IsDBNull(6))
                    {
                        trip.Countries.Add(new CountryDTO
                        {
                            Name = reader.GetString(6)
                        });
                    }
                }
            }
        }

        return trips;
    }
}
