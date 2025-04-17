using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceHistoriesController : ControllerBase
    {
        private readonly IMaintenanceHistoryService _Service;

        public MaintenanceHistoriesController(IMaintenanceHistoryService Service)
        {
            _Service = Service;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllMaintenanceHistories()
        {
            try
            {
                var costs = await _Service.GetAllMaintenanceHistories();
                return Ok(costs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetMaintenanceHistoryById(int id)
        {
            try
            {
                var cost = await _Service.GetMaintenanceHistoryById(id);
                if (cost == null)
                {
                    return NotFound("Maintenance History does not exist");
                }
                return Ok(cost);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateMaintenanceHistory([FromBody] MaintenanceHistoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var maintenanceHistory = await _Service.CreateMaintenanceHistory(request);
                if (maintenanceHistory == null)
                {
                    return BadRequest("Failed to create maintenance history.");
                }
                return CreatedAtAction(nameof(GetMaintenanceHistoryById), new { id = maintenanceHistory.maintenance_id }, maintenanceHistory);
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
        public async Task<ActionResult> UpdateMaintenanceHistory(int id, [FromBody] MaintenanceHistoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingCost = await _Service.GetMaintenanceHistoryById(id);
                if (existingCost == null)
                {
                    return NotFound("Maintenance history does not exist");
                }

                var updatedCost = await _Service.UpdateMaintenanceHistory(id, request);
                if (updatedCost == null)
                {
                    return BadRequest("Failed to update maintenance history.");
                }
                return Ok(updatedCost); // Hoặc NoContent() nếu không cần trả dữ liệu
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
        public async Task<ActionResult> DeleteMaintenanceHistory(int id)
        {
            try
            {
                var existingCost = await _Service.GetMaintenanceHistoryById(id);
                if (existingCost == null)
                {
                    return NotFound("Maintenance history does not exist");
                }

                var result = await _Service.DeleteMaintenanceHistory(id);
                if (!result)
                {
                    return BadRequest("Failed to delete maintenance history.");
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
