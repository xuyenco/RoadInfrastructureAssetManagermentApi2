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
    public class BudgetsController : ControllerBase
    {
        private readonly IBudgetsService _Service;

        public BudgetsController(IBudgetsService Service)
        {
            _Service = Service;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllBudgets()
        {
            try
            {
                var budgets = await _Service.GetAllBudgets();
                return Ok(budgets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetBudgetsById(int id)
        {
            try
            {
                var budget = await _Service.GetBudgetById(id);
                if (budget == null)
                {
                    return NotFound("Budget does not exist");
                }
                return Ok(budget);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateBudgets([FromBody] BudgetsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var budget = await _Service.CreateBudget(request);
                if (budget == null)
                {
                    return BadRequest("Failed to create budget.");
                }
                return CreatedAtAction(nameof(GetBudgetsById), new { id = budget.budget_id }, budget);
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
        public async Task<ActionResult> UpdateBudgets(int id, [FromBody] BudgetsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingBudget = await _Service.GetBudgetById(id);
                if (existingBudget == null)
                {
                    return NotFound("Budget does not exist");
                }

                var updatedBudget = await _Service.UpdateBudget(id, request);
                if (updatedBudget == null)
                {
                    return BadRequest("Failed to update budget.");
                }
                return Ok(updatedBudget); // Hoặc NoContent() nếu không cần trả dữ liệu
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
        public async Task<ActionResult> DeleteBudgets(int id)
        {
            try
            {
                var existingBudget = await _Service.GetBudgetById(id);
                if (existingBudget == null)
                {
                    return NotFound("Budget does not exist");
                }

                var result = await _Service.DeleteBudget(id);
                if (!result)
                {
                    return BadRequest("Failed to delete budget.");
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