using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;

namespace Road_Infrastructure_Asset_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssetCagetoriesController : ControllerBase
    {
        private readonly IAssetCagetoriesService _Service;
        public AssetCagetoriesController(IAssetCagetoriesService Service)
        {
            _Service = Service;
        }
        [HttpGet]
        public async Task<ActionResult> GetAllAssetCagetories()
        {
            return Ok(await _Service.GetAllAssetCagetories());
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetAssetCagetoriesById(int id)
        {
            var Dichvu = await _Service.GetAssetCagetoriesByid(id);
            if (Dichvu == null)
            {
                return NotFound("Asset cagetories does't exist");
            }
            return Ok(Dichvu);
        }

        [HttpPost]
        public async Task<ActionResult> CreateAssetCagetories(AssetCagetoriesRequest request)
        {
            var NewDichvu = await _Service.CreateAssetCagetories(request);
            if (NewDichvu == null)
            {
                return BadRequest();
            }
            return Ok(NewDichvu);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateAssetCagetories(AssetCagetoriesRequest request, int id)
        {
            var currentTrafficLight = await _Service.GetAssetCagetoriesByid(id);
            if (currentTrafficLight == null)
            {
                return NotFound();
            }
            var UpdatedTrafficLight = await _Service.UpdateAssetCagetories(id, request);
            if (UpdatedTrafficLight == null)
            {
                return BadRequest();
            }
            return Ok(UpdatedTrafficLight);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAssetCagetories(int id)
        {
            var currentTrafficLight = await _Service.GetAssetCagetoriesByid(id);
            if (currentTrafficLight == null)
            {
                return NotFound();
            }
            var result = await _Service.DeleteAssetCagetories(id);
            if (result != true)
            {
                return BadRequest();
            }
            return NoContent();

        }
    }
}
