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
    public class AssetsController : ControllerBase
    {
        private readonly IAssetsService _Service;

        public AssetsController(IAssetsService Service)
        {
            _Service = Service;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllAssets()
        {
            try
            {
                var assets = await _Service.GetAllAssets();
                return Ok(assets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetAssetsById(int id)
        {
            try
            {
                var asset = await _Service.GetAssetById(id);
                if (asset == null)
                {
                    return NotFound("Asset does not exist");
                }
                return Ok(asset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateAssets([FromBody] AssetsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var asset = await _Service.CreateAsset(request);
                if (asset == null)
                {
                    return BadRequest("Failed to create asset.");
                }
                return CreatedAtAction(nameof(GetAssetsById), new { id = asset.asset_id }, asset);
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
        public async Task<ActionResult> UpdateAssets(int id, [FromBody] AssetsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingAsset = await _Service.GetAssetById(id);
                if (existingAsset == null)
                {
                    return NotFound("Asset does not exist");
                }

                var updatedAsset = await _Service.UpdateAsset(id, request);
                if (updatedAsset == null)
                {
                    return BadRequest("Failed to update asset.");
                }
                return Ok(updatedAsset); // Hoặc NoContent() nếu không cần trả dữ liệu
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
        public async Task<ActionResult> DeleteAssets(int id)
        {
            try
            {
                var existingAsset = await _Service.GetAssetById(id);
                if (existingAsset == null)
                {
                    return NotFound("Asset does not exist");
                }

                var result = await _Service.DeleteAsset(id);
                if (!result)
                {
                    return BadRequest("Failed to delete asset.");
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