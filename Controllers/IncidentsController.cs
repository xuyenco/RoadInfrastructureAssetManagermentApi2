using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;

namespace Road_Infrastructure_Asset_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentsController : ControllerBase
    {
        private readonly IIncidentsService _Service;
        public IncidentsController(IIncidentsService Service)
        {
            _Service = Service;
        }
        [HttpGet]
        public async Task<ActionResult> GetAllIncidents()
        {
            return Ok(await _Service.GetAllIncidents());
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetIncidentsById(int id)
        {
            var budgets = await _Service.GetIncidentById(id);
            if (budgets == null)
            {
                return NotFound("Incidents does't exist");
            }
            return Ok(budgets);
        }

        [HttpPost]
        public async Task<ActionResult> CreateIncidents(IncidentsRequest request)
        {
            var budget = await _Service.CreateIncident(request);
            if (budget == null)
            {
                return BadRequest();
            }
            return Ok(budget);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateIncidents(IncidentsRequest request, int id)
        {
            var budget = await _Service.GetIncidentById(id);
            if (budget == null)
            {
                return NotFound();
            }
            var newbudget = await _Service.UpdateIncident(id, request);
            if (newbudget == null)
            {
                return BadRequest();
            }
            return Ok(newbudget);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteIncidents(int id)
        {
            var budget = await _Service.GetIncidentById(id);
            if (budget == null)
            {
                return NotFound();
            }
            var result = await _Service.DeleteIncident(id);
            if (result != true)
            {
                return BadRequest();
            }
            return NoContent();

        }
    }
}
