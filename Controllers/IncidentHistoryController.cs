using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;

namespace Road_Infrastructure_Asset_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentHistoryController : ControllerBase
    {
        private readonly IIncidentHistoryService _Service;
        public IncidentHistoryController(IIncidentHistoryService Service)
        {
            _Service = Service;
        }
        [HttpGet]
        public async Task<ActionResult> GetAllIncidentHistory()
        {
            return Ok(await _Service.GetAllIncidentHistory());
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetIncidentHistoryById(int id)
        {
            var incident_history = await _Service.GetIncidentHistoryById(id);
            if (incident_history == null)
            {
                return NotFound("Budgets does't exist");
            }
            return Ok(incident_history);
        }

        [HttpPost]
        public async Task<ActionResult> CreateIncidentHistory(IncidentHistoryRequest request)
        {
            var incident_history = await _Service.CreateIncidentHistory(request);
            if (incident_history == null)
            {
                return BadRequest();
            }
            return Ok(incident_history);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateIncidentHistory(IncidentHistoryRequest request, int id)
        {
            var incident_history = await _Service.GetIncidentHistoryById(id);
            if (incident_history == null)
            {
                return NotFound();
            }
            var newincident_history = await _Service.UpdateIncidentHistory(id, request);
            if (newincident_history == null)
            {
                return BadRequest();
            }
            return Ok(newincident_history);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteIncidentHistory(int id)
        {
            var incident_history = await _Service.GetIncidentHistoryById(id);
            if (incident_history == null)
            {
                return NotFound();
            }
            var result = await _Service.DeleteIncidentHistory(id);
            if (result != true)
            {
                return BadRequest();
            }
            return NoContent();

        }
    }
}
