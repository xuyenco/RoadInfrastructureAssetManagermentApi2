using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;

namespace Road_Infrastructure_Asset_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            return Ok(await _Service.GetAllBudgets());
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetBudgetsById(int id)
        {
            var budgets = await _Service.GetBudgetById(id);
            if (budgets == null)
            {
                return NotFound("Budgets does't exist");
            }
            return Ok(budgets);
        }

        [HttpPost]
        public async Task<ActionResult> CreateBudgets(BudgetsRequest request)
        {
            var budget = await _Service.CreateBudget(request);
            if (budget == null)
            {
                return BadRequest();
            }
            return Ok(budget);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateBudgets(BudgetsRequest request, int id)
        {
            var budget = await _Service.GetBudgetById(id);
            if (budget == null)
            {
                return NotFound();
            }
            var newbudget = await _Service.UpdateBudget(id, request);
            if (newbudget == null)
            {
                return BadRequest();
            }
            return Ok(newbudget);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBudgets(int id)
        {
            var budget = await _Service.GetBudgetById(id);
            if (budget == null)
            {
                return NotFound();
            }
            var result = await _Service.DeleteBudget(id);
            if (result != true)
            {
                return BadRequest();
            }
            return NoContent();

        }
    }
}
