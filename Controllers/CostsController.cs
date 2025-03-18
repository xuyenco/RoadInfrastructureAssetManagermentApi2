using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;

namespace Road_Infrastructure_Asset_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CostsController : ControllerBase
    {
        private readonly ICostsService _Service;
        public CostsController(ICostsService Service)
        {
            _Service = Service;
        }
        [HttpGet]
        public async Task<ActionResult> GetAllCosts()
        {
            return Ok(await _Service.GetAllCosts());
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetCostsById(int id)
        {
            var costs = await _Service.GetCostById(id);
            if (costs == null)
            {
                return NotFound("Costs does't exist");
            }
            return Ok(costs);
        }

        [HttpPost]
        public async Task<ActionResult> CreateCosts(CostsRequest request)
        {
            var cost = await _Service.CreateCost(request);
            if (cost == null)
            {
                return BadRequest();
            }
            return Ok(cost);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateCosts(CostsRequest request, int id)
        {
            var cost = await _Service.GetCostById(id);
            if (cost == null)
            {
                return NotFound();
            }
            var newcost = await _Service.UpdateCost(id, request);
            if (newcost == null)
            {
                return BadRequest();
            }
            return Ok(newcost);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCosts(int id)
        {
            var cost = await _Service.GetCostById(id);
            if (cost == null)
            {
                return NotFound();
            }
            var result = await _Service.DeleteCost(id);
            if (result != true)
            {
                return BadRequest();
            }
            return NoContent();

        }
    }
}
