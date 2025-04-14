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
            try
            {
                var tasks = await _Service.GetAllTasks();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetTaskById(int id)
        {
            try
            {
                var task = await _Service.GetTaskById(id);
                if (task == null)
                {
                    return NotFound("Task does not exist");
                }
                return Ok(task);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateTask([FromBody] TasksRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var task = await _Service.CreateTask(request);
                if (task == null)
                {
                    return BadRequest("Failed to create task.");
                }
                return CreatedAtAction(nameof(GetTaskById), new { id = task.task_id }, task);
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
                // Ghi log lỗi nếu cần
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateTask(int id, [FromBody] TasksRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingTask = await _Service.GetTaskById(id);
                if (existingTask == null)
                {
                    return NotFound("Task does not exist");
                }

                var updatedTask = await _Service.UpdateTask(id, request);
                if (updatedTask == null)
                {
                    return BadRequest("Failed to update task.");
                }
                return Ok(updatedTask); // Hoặc NoContent() nếu không cần trả về dữ liệu
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
                // Ghi log lỗi nếu cần
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTask(int id)
        {
            try
            {
                var existingTask = await _Service.GetTaskById(id);
                if (existingTask == null)
                {
                    return NotFound("Task does not exist");
                }

                var result = await _Service.DeleteTask(id);
                if (!result)
                {
                    return BadRequest("Failed to delete task.");
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    return Conflict(ex.Message); // 409 Conflict cho lỗi khóa ngoại
                }
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}