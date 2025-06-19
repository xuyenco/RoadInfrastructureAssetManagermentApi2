using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BudgetsController : ControllerBase
    {
        private readonly IBudgetsService _Service;
        private readonly ILogger<BudgetsController> _logger; 

        public BudgetsController(IBudgetsService Service, ILogger<BudgetsController> logger) 
        {
            _Service = Service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllBudgets()
        {
            try
            {
                _logger.LogInformation("Received request to get all budgets");
                var budgets = await _Service.GetAllBudgets();
                _logger.LogInformation("Returned {Count} budgets", budgets.Count()); 
                return Ok(budgets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all budgets");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetBudgetsById(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get budget with ID {BudgetId}", id);
                var budget = await _Service.GetBudgetById(id);
                if (budget == null)
                {
                    _logger.LogWarning("Budget with ID {BudgetId} not found", id); 
                    return NotFound("Budget does not exist");
                }
                _logger.LogInformation("Returned budget with ID {BudgetId}", id); 
                return Ok(budget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get budget with ID {BudgetId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateBudgets([FromBody] BudgetsRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model for creating budget"); 
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Received request to create budget"); 
                var budget = await _Service.CreateBudget(request);
                if (budget == null)
                {
                    _logger.LogError("Failed to create budget"); 
                    return BadRequest("Failed to create budget.");
                }
                _logger.LogInformation("Created budget with ID {BudgetId} successfully", budget.budget_id); 
                return CreatedAtAction(nameof(GetBudgetsById), new { id = budget.budget_id }, budget);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for creating budget: {Message}", ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to create budget: {Message}", ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating budget"); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateBudgets(int id, [FromBody] BudgetsRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model for updating budget with ID {BudgetId}", id); 
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Received request to update budget with ID {BudgetId}", id);
                var existingBudget = await _Service.GetBudgetById(id);
                if (existingBudget == null)
                {
                    _logger.LogWarning("Budget with ID {BudgetId} not found for update", id); 
                    return NotFound("Budget does not exist");
                }

                var updatedBudget = await _Service.UpdateBudget(id, request);
                if (updatedBudget == null)
                {
                    _logger.LogError("Failed to update budget with ID {BudgetId}", id); 
                    return BadRequest("Failed to update budget.");
                }
                _logger.LogInformation("Updated budget with ID {BudgetId} successfully", id); 
                return Ok(updatedBudget); 
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for updating budget with ID {BudgetId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to update budget with ID {BudgetId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating budget with ID {BudgetId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBudgets(int id)
        {
            try
            {
                _logger.LogInformation("Received request to delete budget with ID {BudgetId}", id); 
                var existingBudget = await _Service.GetBudgetById(id);
                if (existingBudget == null)
                {
                    _logger.LogWarning("Budget with ID {BudgetId} not found for deletion", id); 
                    return NotFound("Budget does not exist");
                }

                var result = await _Service.DeleteBudget(id);
                if (!result)
                {
                    _logger.LogError("Failed to delete budget with ID {BudgetId}", id); 
                    return BadRequest("Failed to delete budget.");
                }
                _logger.LogInformation("Deleted budget with ID {BudgetId} successfully", id); 
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    _logger.LogError(ex, "Failed to delete budget with ID {BudgetId}: {Message}", id, ex.Message);
                    return Conflict(ex.Message);
                }
                _logger.LogError(ex, "Failed to delete budget with ID {BudgetId}: {Message}", id, ex.Message); 
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting budget with ID {BudgetId}", id); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}