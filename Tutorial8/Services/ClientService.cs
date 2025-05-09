using System.Globalization;
using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class ClientService : IClientService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=apbd;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";    // Retrieve all trips for a specified client, along with registration and payment details

    public async Task<List<ClientTripDTO>> GetTripsForClient(int id)
    {
        var trips = new List<ClientTripDTO>();

        // Retrieve all rows of trip and needed client data
        string command = @"
    SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
           ct.RegisteredAt, ct.PaymentDate
    FROM Trip t
    JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip
    WHERE ct.IdClient = @ClientId"; // Ensure the parameter @ClientId is referenced correctly

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            // Add the @ClientId parameter to the SQL command
            cmd.Parameters.AddWithValue("@ClientId", id);

            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    trips.Add(new ClientTripDTO
                    {
                        IdTrip = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        DateFrom = reader.GetDateTime(3),
                        DateTo = reader.GetDateTime(4),
                        MaxPeople = reader.GetInt32(5),
                        RegisteredAt = reader.IsDBNull(6) 
                            ? (DateTime?)null 
                            : DateTime.ParseExact(reader.GetInt32(6).ToString(), "yyyyMMdd", CultureInfo.InvariantCulture),  // Convert INT to DateTime
                        PaymentDate = reader.IsDBNull(7) 
                            ? (DateTime?)null 
                            : DateTime.ParseExact(reader.GetInt32(7).ToString(), "yyyyMMdd", CultureInfo.InvariantCulture)  // Convert INT to DateTime
                    });
                }
            }
        }
        return trips;
    }


    //Creation of a client, also returns his id after
    public async Task<int> CreateClient(CreateClientDTO client)
    {
        //Insert data into each column of client table
        const string query = @"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@FirstName", client.FirstName);
            cmd.Parameters.AddWithValue("@LastName", client.LastName);
            cmd.Parameters.AddWithValue("@Email", client.Email);
            cmd.Parameters.AddWithValue("@Telephone", client.Telephone);
            cmd.Parameters.AddWithValue("@Pesel", client.Pesel);
            
            await conn.OpenAsync();
            var id = (int) await cmd.ExecuteScalarAsync();
            return id;
        }
    }
    public async Task<ServiceResult> RegisterClientForTrip(int clientId, int tripId)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
    
        // Check if the client with the given ID exists
        var checkClient = new SqlCommand("SELECT COUNT(*) FROM Client WHERE IdClient = @Id", conn);
        checkClient.Parameters.AddWithValue("@Id", clientId);
        if ((int)await checkClient.ExecuteScalarAsync() == 0)
        {
            return new ServiceResult { Success = false, Message = "Client does not exist." };
        }
    
        // Check if the trip with the given ID exists and retrieves its maximum capacity
        var checkTrip = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @Id", conn);
        checkTrip.Parameters.AddWithValue("@Id", tripId);
        var maxPeopleObj = await checkTrip.ExecuteScalarAsync();
        if (maxPeopleObj == null)
        {
            return new ServiceResult { Success = false, Message = "Trip does not exist." };
        }
        int maxPeople = (int)maxPeopleObj;
    
        // Count the number of clients already registered for the trip
        var countCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @TripId", conn);
        countCmd.Parameters.AddWithValue("@TripId", tripId);
        int registered = (int)await countCmd.ExecuteScalarAsync();
    
        if (registered >= maxPeople)
        {
            return new ServiceResult { Success = false, Message = "Trip is full." };
        }
    
        // Convert DateTime to integer in yyyyMMdd format for RegisteredAt
        int registeredAt = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
    
        // Register the client for the specified trip
        var insertCmd = new SqlCommand(@"
            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
            VALUES (@ClientId, @TripId, @RegisteredAt, @PaymentDate)", conn);
    
        // Set the parameters
        insertCmd.Parameters.AddWithValue("@ClientId", clientId);
        insertCmd.Parameters.AddWithValue("@TripId", tripId);
        insertCmd.Parameters.AddWithValue("@RegisteredAt", registeredAt);  // RegisteredAt as integer
    
        // If PaymentDate is null, use DBNull.Value
        insertCmd.Parameters.AddWithValue("@PaymentDate", DBNull.Value);  // Assuming payment has not been made yet
    
        await insertCmd.ExecuteNonQueryAsync();
    
        return new ServiceResult { Success = true, Message = "Client registered successfully." };
    }


    //Remove a client from a specific trip if they are registered
    public async Task<bool> RemoveClientFromTrip(int clientId, int tripId)
    {
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    //Check if the client is registered for the trip
    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdClient = @CId AND IdTrip = @TId", conn);
    checkCmd.Parameters.AddWithValue("@CId", clientId);
    checkCmd.Parameters.AddWithValue("@TId", tripId);
    if ((int)await checkCmd.ExecuteScalarAsync() == 0)
    {
        return false;
    }
    //Remove the client from the trip
    var deleteCmd = new SqlCommand("DELETE FROM Client_Trip WHERE IdClient = @CId AND IdTrip = @TId", conn);
    deleteCmd.Parameters.AddWithValue("@CId", clientId);
    deleteCmd.Parameters.AddWithValue("@TId", tripId);
    await deleteCmd.ExecuteNonQueryAsync();

    return true;
    }

} 