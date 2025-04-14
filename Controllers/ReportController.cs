using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management_2.Interface;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _Service;
        private readonly IConfiguration _Configuration;
        public ReportController(IReportService Service, IConfiguration configuration)
        {
            _Service = Service;
            _Configuration = configuration;
        }
        [HttpGet("TaskStatusDistribution")]
        public async Task<ActionResult> GetTaskStatusDistribution()
        {
            return Ok(await _Service.GetTaskStatusDistributions());
        }
        [HttpGet("IncidentTypeDistribution")]
        public async Task<ActionResult> GetIncidentTypeDistribution()
        {
            return Ok(await _Service.GetIncidentTypeDistributions());
        }
        [HttpGet("IncidentsOverTime")]
        public async Task<ActionResult> GetIncidentsOverTime()
        {
            return Ok(await _Service.GetIncidentsOverTime());
        }
        [HttpGet("BudgetAndCosts")]
        public async Task<ActionResult> GetBudgetAndCosts()
        {
            return Ok(await _Service.GetBudgetAndCosts());
        }
        [HttpGet("AssetDistributionByCategories")]
        public async Task<ActionResult> GetAssetDistributionByCategories()
        {
            return Ok(await _Service.GetAssetDistributionByCategories());
        }
        [HttpGet("AssetDistributedByCondition")]
        public async Task<ActionResult> GetAssetDistributedByCondition()
        {
            return Ok(await _Service.GetAssetDistributedByCondition());
        }
    }
}
