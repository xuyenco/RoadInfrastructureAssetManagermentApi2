using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; 
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class CostsController : ControllerBase
    {
        private readonly ICostsService _Service;
        private readonly ILogger<CostsController> _logger; 

        public CostsController(ICostsService Service, ILogger<CostsController> logger) 
        {
            _Service = Service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllCosts()
        {
            try
            {
                _logger.LogInformation("Received request to get all costs");
                var costs = await _Service.GetAllCosts();
                _logger.LogInformation("Returned {Count} costs", costs.Count()); 
                return Ok(costs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all costs"); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetCostsById(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get cost with ID {CostId}", id); 
                var cost = await _Service.GetCostById(id);
                if (cost == null)
                {
                    _logger.LogWarning("Cost with ID {CostId} not found", id);
                    return NotFound("Cost does not exist");
                }
                _logger.LogInformation("Returned cost with ID {CostId}", id); 
                return Ok(cost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cost with ID {CostId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateCosts([FromBody] CostsRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model for creating cost"); 
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Received request to create cost"); 
                var cost = await _Service.CreateCost(request);
                if (cost == null)
                {
                    _logger.LogError("Failed to create cost"); 
                    return BadRequest("Failed to create cost.");
                }
                _logger.LogInformation("Created cost with ID {CostId} successfully", cost.cost_id); 
                return CreatedAtAction(nameof(GetCostsById), new { id = cost.cost_id }, cost);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for creating cost: {Message}", ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to create cost: {Message}", ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating cost"); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateCosts(int id, [FromBody] CostsRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model for updating cost with ID {CostId}", id); 
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Received request to update cost with ID {CostId}", id); 
                var existingCost = await _Service.GetCostById(id);
                if (existingCost == null)
                {
                    _logger.LogWarning("Cost with ID {CostId} not found for update", id); 
                    return NotFound("Cost does not exist");
                }

                var updatedCost = await _Service.UpdateCost(id, request);
                if (updatedCost == null)
                {
                    _logger.LogError("Failed to update cost with ID {CostId}", id); 
                    return BadRequest("Failed to update cost.");
                }
                _logger.LogInformation("Updated cost with ID {CostId} successfully", id);
                return Ok(updatedCost); 
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for updating cost with ID {CostId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to update cost with ID {CostId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating cost with ID {CostId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCosts(int id)
        {
            try
            {
                _logger.LogInformation("Received request to delete cost with ID {CostId}", id); 
                var existingCost = await _Service.GetCostById(id);
                if (existingCost == null)
                {
                    _logger.LogWarning("Cost with ID {CostId} not found for deletion", id);
                    return NotFound("Cost does not exist");
                }

                var result = await _Service.DeleteCost(id);
                if (!result)
                {
                    _logger.LogError("Failed to delete cost with ID {CostId}", id); 
                    return BadRequest("Failed to delete cost.");
                }
                _logger.LogInformation("Deleted cost with ID {CostId} successfully", id); 
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    _logger.LogError(ex, "Failed to delete cost with ID {CostId}: {Message}", id, ex.Message); 
                    return Conflict(ex.Message);
                }
                _logger.LogError(ex, "Failed to delete cost with ID {CostId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting cost with ID {CostId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}