using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentsController : ControllerBase
    {
        private readonly IIncidentsService _Service;
        private readonly ILogger<IncidentsController> _logger; 

        public IncidentsController(IIncidentsService Service, ILogger<IncidentsController> logger) 
        {
            _Service = Service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllIncidents()
        {
            try
            {
                _logger.LogInformation("Received request to get all incidents"); 
                var incidents = await _Service.GetAllIncidents();
                _logger.LogInformation("Returned {Count} incidents", incidents.Count()); 
                return Ok(incidents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all incidents"); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetIncidentsById(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get incident with ID {IncidentId}", id); 
                var incident = await _Service.GetIncidentById(id);
                if (incident == null)
                {
                    _logger.LogWarning("Incident with ID {IncidentId} not found", id); 
                    return NotFound("Incident does not exist");
                }
                _logger.LogInformation("Returned incident with ID {IncidentId}", id); 
                return Ok(incident);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get incident with ID {IncidentId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("paged")]
        public async Task<ActionResult> GetIncidentsPagination(int page = 1, int pageSize = 1, string searchTerm = "", int searchField = 0)
        {
            try
            {
                _logger.LogInformation("Received request to get incidents with pagination - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, SearchField: {SearchField}",
                    page, pageSize, searchTerm, searchField);

                var (incidents, totalCount) = await _Service.GetIncidentsPagination(page, pageSize, searchTerm, searchField);

                if (incidents == null || !incidents.Any())
                {
                    _logger.LogWarning("No incidents found for Page: {Page}, SearchTerm: {SearchTerm}, SearchField: {SearchField}",
                        page, searchTerm, searchField);
                    return Ok(new {incidents,totalCount});
                }

                _logger.LogInformation("Returned {IncidentsCount} users for Page: {Page}, TotalCount: {TotalCount}",
                    incidents.Count(), page, totalCount);

                return Ok(new { incidents, totalCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get incidents with pagination - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, SearchField: {SearchField}",
                    page, pageSize, searchTerm, searchField);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateIncidents([FromBody] IncidentsRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model for creating incident"); 
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Received request to create incident"); 
                var incident = await _Service.CreateIncident(request);
                _logger.LogInformation("Created incident with ID {IncidentId} successfully", incident.incident_id); 
                return CreatedAtAction(nameof(GetIncidentsById), new { id = incident.incident_id }, incident);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for creating incident: {Message}", ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to create incident: {Message}", ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating incident"); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "admin,inspector")]
        public async Task<ActionResult> UpdateIncidents(int id, [FromBody] IncidentsRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model for updating incident with ID {IncidentId}", id); 
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Received request to update incident with ID {IncidentId}", id); 
                var existingIncident = await _Service.GetIncidentById(id);
                if (existingIncident == null)
                {
                    _logger.LogWarning("Incident with ID {IncidentId} not found for update", id); 
                    return NotFound("Incident does not exist");
                }

                var updatedIncident = await _Service.UpdateIncident(id, request);
                if (updatedIncident == null)
                {
                    _logger.LogError("Failed to update incident with ID {IncidentId}", id);
                    return BadRequest("Failed to update incident.");
                }
                _logger.LogInformation("Updated incident with ID {IncidentId} successfully", id); 
                return Ok(updatedIncident); 
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for updating incident with ID {IncidentId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to update incident with ID {IncidentId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating incident with ID {IncidentId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,inspector")]
        public async Task<ActionResult> DeleteIncidents(int id)
        {
            try
            {
                _logger.LogInformation("Received request to delete incident with ID {IncidentId}", id); 
                var existingIncident = await _Service.GetIncidentById(id);
                if (existingIncident == null)
                {
                    _logger.LogWarning("Incident with ID {IncidentId} not found for deletion", id); 
                    return NotFound("Incident does not exist");
                }

                var result = await _Service.DeleteIncident(id);
                if (!result)
                {
                    _logger.LogError("Failed to delete incident with ID {IncidentId}", id); 
                    return BadRequest("Failed to delete incident.");
                }
                _logger.LogInformation("Deleted incident with ID {IncidentId} successfully", id); 
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    _logger.LogError(ex, "Failed to delete incident with ID {IncidentId}: {Message}", id, ex.Message);
                    return Conflict(ex.Message); 
                }
                _logger.LogError(ex, "Failed to delete incident with ID {IncidentId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting incident with ID {IncidentId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}