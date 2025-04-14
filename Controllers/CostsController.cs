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
            try
            {
                var costs = await _Service.GetAllCosts();
                return Ok(costs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetCostsById(int id)
        {
            try
            {
                var cost = await _Service.GetCostById(id);
                if (cost == null)
                {
                    return NotFound("Cost does not exist");
                }
                return Ok(cost);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateCosts([FromBody] CostsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var cost = await _Service.CreateCost(request);
                if (cost == null)
                {
                    return BadRequest("Failed to create cost.");
                }
                return CreatedAtAction(nameof(GetCostsById), new { id = cost.cost_id }, cost);
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
        public async Task<ActionResult> UpdateCosts(int id, [FromBody] CostsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingCost = await _Service.GetCostById(id);
                if (existingCost == null)
                {
                    return NotFound("Cost does not exist");
                }

                var updatedCost = await _Service.UpdateCost(id, request);
                if (updatedCost == null)
                {
                    return BadRequest("Failed to update cost.");
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
        public async Task<ActionResult> DeleteCosts(int id)
        {
            try
            {
                var existingCost = await _Service.GetCostById(id);
                if (existingCost == null)
                {
                    return NotFound("Cost does not exist");
                }

                var result = await _Service.DeleteCost(id);
                if (!result)
                {
                    return BadRequest("Failed to delete cost.");
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