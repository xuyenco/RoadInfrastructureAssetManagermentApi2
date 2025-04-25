using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; 
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
        private readonly ILogger<ReportController> _logger; 

        public ReportController(IReportService Service, IConfiguration configuration, ILogger<ReportController> logger) 
        {
            _Service = Service;
            _Configuration = configuration;
            _logger = logger;
        }

        [HttpGet("TaskStatusDistribution")]
        public async Task<ActionResult> GetTaskStatusDistribution()
        {
            try
            {
                _logger.LogInformation("Received request to get task status distribution"); 
                var result = await _Service.GetTaskStatusDistributions();
                _logger.LogInformation("Returned {Count} task status distributions", result.Count()); 
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get task status distribution");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("IncidentTypeDistribution")]
        public async Task<ActionResult> GetIncidentTypeDistribution()
        {
            try
            {
                _logger.LogInformation("Received request to get incident type distribution"); 
                var result = await _Service.GetIncidentTypeDistributions();
                _logger.LogInformation("Returned {Count} incident type distributions", result.Count()); 
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get incident type distribution"); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("IncidentsOverTime")]
        public async Task<ActionResult> GetIncidentsOverTime()
        {
            try
            {
                _logger.LogInformation("Received request to get incidents over time");
                var result = await _Service.GetIncidentsOverTime();
                _logger.LogInformation("Returned {Count} incidents over time", result.Count()); 
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get incidents over time"); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("BudgetAndCosts")]
        public async Task<ActionResult> GetBudgetAndCosts()
        {
            try
            {
                _logger.LogInformation("Received request to get budget and costs"); 
                var result = await _Service.GetBudgetAndCosts();
                _logger.LogInformation("Returned {Count} budget and cost records", result.Count()); 
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get budget and costs"); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("AssetDistributionByCategories")]
        public async Task<ActionResult> GetAssetDistributionByCategories()
        {
            try
            {
                _logger.LogInformation("Received request to get asset distribution by categories"); 
                var result = await _Service.GetAssetDistributionByCategories();
                _logger.LogInformation("Returned {Count} asset distributions by category", result.Count()); 
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get asset distribution by categories");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("AssetDistributedByCondition")]
        public async Task<ActionResult> GetAssetDistributedByCondition()
        {
            try
            {
                _logger.LogInformation("Received request to get asset distribution by condition"); 
                var result = await _Service.GetAssetDistributedByCondition();
                _logger.LogInformation("Returned {Count} asset distributions by condition", result.Count()); 
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get asset distribution by condition"); 
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}