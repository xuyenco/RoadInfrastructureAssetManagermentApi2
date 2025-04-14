using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using System;
using System.Threading.Tasks;

namespace Road_Infrastructure_Asset_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class IncidentHistoryController : ControllerBase
    {
        private readonly IIncidentHistoryService _Service;

        public IncidentHistoryController(IIncidentHistoryService Service)
        {
            _Service = Service;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllIncidentHistory()
        {
            try
            {
                var histories = await _Service.GetAllIncidentHistory();
                return Ok(histories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("incidentid/{id}")]
        public async Task<ActionResult> GetIncidentHistoryById(int id)
        {
            try
            {
                var histories = await _Service.GetIncidentHistoryByIncidentID(id);
                return Ok(histories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetIncidentHistoryByIncidentId(int id)
        {
            try
            {
                var history = await _Service.GetIncidentHistoryById(id);
                if (history == null)
                {
                    return NotFound("Incident history does not exist");
                }
                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateIncidentHistory([FromBody] IncidentHistoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var history = await _Service.CreateIncidentHistory(request);
                if (history == null)
                {
                    return BadRequest("Failed to create incident history.");
                }
                return CreatedAtAction(nameof(GetIncidentHistoryById), new { id = history.history_id }, history);
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
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateIncidentHistory(int id, [FromBody] IncidentHistoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingHistory = await _Service.GetIncidentHistoryById(id);
                if (existingHistory == null)
                {
                    return NotFound("Incident history does not exist");
                }

                var updatedHistory = await _Service.UpdateIncidentHistory(id, request);
                if (updatedHistory == null)
                {
                    return BadRequest("Failed to update incident history.");
                }
                return Ok(updatedHistory); // Hoặc NoContent() nếu không cần trả dữ liệu
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
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteIncidentHistory(int id)
        {
            try
            {
                var existingHistory = await _Service.GetIncidentHistoryById(id);
                if (existingHistory == null)
                {
                    return NotFound("Incident history does not exist");
                }

                var result = await _Service.DeleteIncidentHistory(id);
                if (!result)
                {
                    return BadRequest("Failed to delete incident history.");
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    return Conflict(ex.Message);
                }
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}