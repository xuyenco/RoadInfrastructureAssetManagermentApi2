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
        private readonly IReportService _service;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReportController> _logger;

        public ReportController(IReportService service, IConfiguration configuration, ILogger<ReportController> logger)
        {
            _service = service;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("AssetDistributedByCondition")]
        public async Task<ActionResult> GetAssetDistributedByCondition()
        {
            try
            {
                _logger.LogInformation("Received request to get asset status report");
                var result = await _service.GetAssetDistributedByCondition();
                _logger.LogInformation("Returned {Count} asset status reports", result.Count());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get asset status report");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("IncidentTypeDistribution")]
        public async Task<ActionResult> GetIncidentTypeDistribution()
        {
            try
            {
                _logger.LogInformation("Received request to get incident distribution report");
                var result = await _service.GetIncidentTypeDistribution();
                _logger.LogInformation("Returned {Count} incident distribution reports", result.Count());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get incident distribution report");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("TaskStatusDistribution")]
        public async Task<ActionResult> GetTaskStatusDistribution()
        {
            try
            {
                _logger.LogInformation("Received request to get task performance report");
                var result = await _service.GetTaskStatusDistribution();
                _logger.LogInformation("Returned {Count} task performance reports", result.Count());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get task performance report");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("IncidentsOverTime")]
        public async Task<ActionResult> GetIncidentsOverTime()
        {
            try
            {
                _logger.LogInformation("Received request to get incident and task trend report");
                var result = await _service.GetIncidentsOverTime();
                _logger.LogInformation("Returned {Count} incident and task trend reports", result.Count());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get incident and task trend report");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("MaintenanceFrequency")]
        public async Task<ActionResult> GetMaintenanceFrequency()
        {
            try
            {
                _logger.LogInformation("Received request to get maintenance frequency report");
                var result = await _service.GetMaintenanceFrequencyReport();
                _logger.LogInformation("Returned {Count} maintenance frequency reports", result.Count());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get maintenance frequency report");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}