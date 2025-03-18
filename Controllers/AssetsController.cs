using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;

namespace Road_Infrastructure_Asset_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            return Ok(await _Service.GetAllAssets());
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetAssetsById(int id)
        {
            var assets = await _Service.GetAssetById(id);
            if (assets == null)
            {
                return NotFound("Assets does't exist");
            }
            return Ok(assets);
        }

        [HttpPost]
        public async Task<ActionResult> CreateAssets(AssetsRequest request)
        {
            var asset = await _Service.CreateAsset(request);
            if (asset == null)
            {
                return BadRequest();
            }
            return Ok(asset);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateAssets(AssetsRequest request, int id)
        {
            var asset = await _Service.GetAssetById(id);
            if (asset == null)
            {
                return NotFound();
            }
            var newasset = await _Service.UpdateAsset(id, request);
            if (newasset == null)
            {
                return BadRequest();
            }
            return Ok(newasset);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAssets(int id)
        {
            var asset = await _Service.GetAssetById(id);
            if (asset == null)
            {
                return NotFound();
            }
            var result = await _Service.DeleteAsset(id);
            if (result != true)
            {
                return BadRequest();
            }
            return NoContent();

        }
    }
}
