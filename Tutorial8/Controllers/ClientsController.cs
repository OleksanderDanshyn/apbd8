    using Microsoft.AspNetCore.Mvc;
    using Tutorial8.Models.DTOs;
    using Tutorial8.Services;

    namespace Tutorial8.Controllers;

    [Route("api/clients/")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }
        
        //GET: api/clients/{id}/trips
        //Returns a list of trips for a specific client (by client ID)
        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetTripsForClient(int id)
        {
            var trips = await _clientService.GetTripsForClient(id);
            if (trips == null)
            {
                return NotFound("Client not found or has no trips.");
            }

            return Ok(trips);
        }

        //POST: api/clients
        //Creates a new client
        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientDTO client)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var id = await _clientService.CreateClient(client);
                return CreatedAtAction(nameof(GetTripsForClient), new { id = id }, id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error creating client: " + ex.Message);
            }
        }
        
        //PUT: api/clients/{id}/trips/{tripId}
        //Registers the client with the specified ID for the given trip
        [HttpPut("{id}/trips/{tripId}")]
        public async Task<IActionResult> RegisterClientForTrip(int id, int tripId)
        {
            var result = await _clientService.RegisterClientForTrip(id, tripId);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            return Ok("Client registered successfully.");
        }

        //DELETE: api/clients/{id}/trips/{tripId}
        //Removes the client's registration from a trip
        [HttpDelete("{id}/trips/{tripId}")]
        public async Task<IActionResult> RemoveClientFromTrip(int id, int tripId)
        {
            var success = await _clientService.RemoveClientFromTrip(id, tripId);
            if (!success)
            {
                return NotFound("Client is not registered for this trip.");
            }

            return Ok("Client successfully removed from trip.");
        }
    }