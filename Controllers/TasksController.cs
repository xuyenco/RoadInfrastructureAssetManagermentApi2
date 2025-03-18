using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;

namespace Road_Infrastructure_Asset_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ITasksService _Service;
        public TasksController(ITasksService Service)
        {
            _Service = Service;
        }
        [HttpGet]
        public async Task<ActionResult> GetAllTasks()
        {
            return Ok(await _Service.GetAllTasks());
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetTaskById(int id)
        {
            var tasks = await _Service.GetTaskById(id);
            if (tasks == null)
            {
                return NotFound("Budgets does't exist");
            }
            return Ok(tasks);
        }

        [HttpPost]
        public async Task<ActionResult> CreateTask(TasksRequest request)
        {
            var task = await _Service.CreateTask(request);
            if (task == null)
            {
                return BadRequest();
            }
            return Ok(task);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateTask(TasksRequest request, int id)
        {
            var task = await _Service.GetTaskById(id);
            if (task == null)
            {
                return NotFound();
            }
            var newtask = await _Service.UpdateTask(id, request);
            if (newtask == null)
            {
                return BadRequest();
            }
            return Ok(newtask);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTask(int id)
        {
            var task = await _Service.GetTaskById(id);
            if (task == null)
            {
                return NotFound();
            }
            var result = await _Service.DeleteTask(id);
            if (result != true)
            {
                return BadRequest();
            }
            return NoContent();

        }
    }
}
