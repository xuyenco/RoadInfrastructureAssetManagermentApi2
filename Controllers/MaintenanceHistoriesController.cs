using Microsoft.AspNetCore.Authorization;
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
        private readonly ILogger<MaintenanceHistoriesController> _logger; 

        public MaintenanceHistoriesController(IMaintenanceHistoryService Service, ILogger<MaintenanceHistoriesController> logger) 
        {
            _Service = Service;
            _logger = logger;
        }

        [HttpGet]
        //[Authorize]
        public async Task<ActionResult> GetAllMaintenanceHistories()
        {
            try
            {
                _logger.LogInformation("Received request to get all maintenance histories"); 
                var costs = await _Service.GetAllMaintenanceHistories();
                _logger.LogInformation("Returned {Count} maintenance histories", costs.Count()); 
                return Ok(costs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all maintenance histories"); // Log lỗi
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        //[Authorize]
        public async Task<ActionResult> GetMaintenanceHistoryById(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get maintenance history with ID {MaintenanceId}", id); 
                var cost = await _Service.GetMaintenanceHistoryById(id);
                if (cost == null)
                {
                    _logger.LogWarning("Maintenance history with ID {MaintenanceId} not found", id); 
                    return NotFound("Maintenance History does not exist");
                }
                _logger.LogInformation("Returned maintenance history with ID {MaintenanceId}", id); 
                return Ok(cost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get maintenance history with ID {MaintenanceId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("AssetId/{id}")]
        //[Authorize]
        public async Task<ActionResult> GetMaintenanceHistoryByAssetId(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get maintenance histories for asset ID {AssetId}", id); 
                var costs = await _Service.GetMaintenanceHistoryByAssetId(id);
                _logger.LogInformation("Returned {Count} maintenance histories for asset ID {AssetId}", costs.Count(), id); 
                return Ok(costs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get maintenance histories for asset ID {AssetId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("AssetId/{id}/Paged")]
        //[Authorize]
        public async Task<ActionResult> GetPagedMaintenanceHistoryByAssetId(
            int id,
            [FromQuery] int currentPage = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = "",
            [FromQuery] int searchField = 0)
        {
            try
            {
                _logger.LogInformation(
                    "Received request to get paged maintenance histories for asset ID {AssetId} with parameters: Page={CurrentPage}, PageSize={PageSize}, SearchTerm={SearchTerm}, SearchField={SearchField}",
                    id, currentPage, pageSize, searchTerm, searchField);

                var (maintenanceHistories, totalCount) = await _Service.GetPagedMaintenanceHistoryByAssetId(id, currentPage, pageSize, searchTerm, searchField);

                _logger.LogInformation(
                    "Returned {Count} maintenance histories for asset ID {AssetId} (Page: {CurrentPage}, PageSize: {PageSize}, TotalCount: {TotalCount})",
                    maintenanceHistories.Count(), id, currentPage, pageSize, totalCount);

                return Ok(new
                {
                    maintenanceHistories,
                    totalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to get paged maintenance histories for asset ID {AssetId} with parameters: Page={CurrentPage}, PageSize={PageSize}, SearchTerm={SearchTerm}, SearchField={SearchField}",
                    id, currentPage, pageSize, searchTerm, searchField);
                return StatusCode(500, "An unexpected error occurred while retrieving paged maintenance histories.");
            }
        }

        [HttpPost]
        //[Authorize(Roles = "inspector")]
        public async Task<ActionResult> CreateMaintenanceHistory([FromBody] MaintenanceHistoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model for creating maintenance history"); 
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Received request to create maintenance history for asset ID {AssetId} and task ID {TaskId}", request.asset_id, request.task_id); 
                var maintenanceHistory = await _Service.CreateMaintenanceHistory(request);
                if (maintenanceHistory == null)
                {
                    _logger.LogError("Failed to create maintenance history for asset ID {AssetId} and task ID {TaskId}", request.asset_id, request.task_id);
                    return BadRequest("Failed to create maintenance history.");
                }
                _logger.LogInformation("Created maintenance history with ID {MaintenanceId} successfully", maintenanceHistory.maintenance_id);
                return CreatedAtAction(nameof(GetMaintenanceHistoryById), new { id = maintenanceHistory.maintenance_id }, maintenanceHistory);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for creating maintenance history: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to create maintenance history: {Message}", ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating maintenance history for asset ID {AssetId} and task ID {TaskId}", request.asset_id, request.task_id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        //[Authorize(Roles = "inspector")]
        public async Task<ActionResult> UpdateMaintenanceHistory(int id, [FromBody] MaintenanceHistoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model for updating maintenance history with ID {MaintenanceId}", id); 
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Received request to update maintenance history with ID {MaintenanceId}", id);
                var existingCost = await _Service.GetMaintenanceHistoryById(id);
                if (existingCost == null)
                {
                    _logger.LogWarning("Maintenance history with ID {MaintenanceId} not found for update", id); 
                    return NotFound("Maintenance history does not exist");
                }

                var updatedCost = await _Service.UpdateMaintenanceHistory(id, request);
                if (updatedCost == null)
                {
                    _logger.LogError("Failed to update maintenance history with ID {MaintenanceId}", id); 
                    return BadRequest("Failed to update maintenance history.");
                }
                _logger.LogInformation("Updated maintenance history with ID {MaintenanceId} successfully", id); 
                return Ok(updatedCost); // Hoặc NoContent() nếu không cần trả dữ liệu
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for updating maintenance history with ID {MaintenanceId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to update maintenance history with ID {MaintenanceId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating maintenance history with ID {MaintenanceId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = "inspector")]
        public async Task<ActionResult> DeleteMaintenanceHistory(int id)
        {
            try
            {
                _logger.LogInformation("Received request to delete maintenance history with ID {MaintenanceId}", id); 
                var existingCost = await _Service.GetMaintenanceHistoryById(id);
                if (existingCost == null)
                {
                    _logger.LogWarning("Maintenance history with ID {MaintenanceId} not found for deletion", id); 
                    return NotFound("Maintenance history does not exist");
                }

                var result = await _Service.DeleteMaintenanceHistory(id);
                if (!result)
                {
                    _logger.LogError("Failed to delete maintenance history with ID {MaintenanceId}", id); 
                    return BadRequest("Failed to delete maintenance history.");
                }
                _logger.LogInformation("Deleted maintenance history with ID {MaintenanceId} successfully", id); 
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    _logger.LogError(ex, "Failed to delete maintenance history with ID {MaintenanceId}: {Message}", id, ex.Message); 
                    return Conflict(ex.Message);
                }
                _logger.LogError(ex, "Failed to delete maintenance history with ID {MaintenanceId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting maintenance history with ID {MaintenanceId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}