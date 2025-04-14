using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using System;
using System.Threading.Tasks;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class IncidentsController : ControllerBase
    {
        private readonly IIncidentsService _Service;

        public IncidentsController(IIncidentsService Service)
        {
            _Service = Service;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllIncidents()
        {
            try
            {
                var incidents = await _Service.GetAllIncidents();
                return Ok(incidents);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is configured
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetIncidentsById(int id)
        {
            try
            {
                var incident = await _Service.GetIncidentById(id);
                if (incident == null)
                {
                    return NotFound("Incident does not exist");
                }
                return Ok(incident);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is configured
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateIncidents([FromBody] IncidentsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var incident = await _Service.CreateIncident(request);
                return CreatedAtAction(nameof(GetIncidentsById), new { id = incident.incident_id }, incident);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is configured
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateIncidents(int id, [FromBody] IncidentsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingIncident = await _Service.GetIncidentById(id);
                if (existingIncident == null)
                {
                    return NotFound("Incident does not exist");
                }

                var updatedIncident = await _Service.UpdateIncident(id, request);
                if (updatedIncident == null)
                {
                    return BadRequest("Failed to update incident.");
                }
                return Ok(updatedIncident); // Or return NoContent() if no data is needed
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is configured
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteIncidents(int id)
        {
            try
            {
                var existingIncident = await _Service.GetIncidentById(id);
                if (existingIncident == null)
                {
                    return NotFound("Incident does not exist");
                }

                var result = await _Service.DeleteIncident(id);
                if (!result)
                {
                    return BadRequest("Failed to delete incident.");
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    return Conflict(ex.Message); // 409 Conflict for foreign key issues
                }
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is configured
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}