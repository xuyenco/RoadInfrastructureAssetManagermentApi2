using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using System;
using System.Threading.Tasks;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ITasksService _Service;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITasksService Service, ILogger<TasksController> logger)
        {
            _Service = Service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllTasks()
        {
            try
            {
                _logger.LogInformation("Received request to get all tasks");
                var tasks = await _Service.GetAllTasks();
                _logger.LogInformation("Returned {Count} tasks", tasks.Count());
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all tasks");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetTaskById(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get task with ID {TaskId}", id);
                var task = await _Service.GetTaskById(id);
                if (task == null)
                {
                    _logger.LogWarning("Task with ID {TaskId} not found", id);
                    return NotFound("Task does not exist");
                }
                _logger.LogInformation("Returned task with ID {TaskId}", id);
                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get task with ID {TaskId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateTask([FromBody] TasksRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model for creating task");
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Received request to create task with task_type {TaskType}", request.task_type);
                var task = await _Service.CreateTask(request);
                if (task == null)
                {
                    _logger.LogError("Failed to create task with task_type {TaskType}", request.task_type);
                    return BadRequest("Failed to create task.");
                }
                _logger.LogInformation("Created task with ID {TaskId} successfully", task.task_id);
                return CreatedAtAction(nameof(GetTaskById), new { id = task.task_id }, task);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for creating task: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to create task: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating task");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateTask(int id, [FromBody] TasksRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model for updating task with ID {TaskId}", id);
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Received request to update task with ID {TaskId}", id);
                var existingTask = await _Service.GetTaskById(id);
                if (existingTask == null)
                {
                    _logger.LogWarning("Task with ID {TaskId} not found for update", id);
                    return NotFound("Task does not exist");
                }

                var updatedTask = await _Service.UpdateTask(id, request);
                if (updatedTask == null)
                {
                    _logger.LogError("Failed to update task with ID {TaskId}", id);
                    return BadRequest("Failed to update task.");
                }
                _logger.LogInformation("Updated task with ID {TaskId} successfully", id);
                return Ok(updatedTask);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for updating task with ID {TaskId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to update task with ID {TaskId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating task with ID {TaskId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTask(int id)
        {
            try
            {
                _logger.LogInformation("Received request to delete task with ID {TaskId}", id);
                var existingTask = await _Service.GetTaskById(id);
                if (existingTask == null)
                {
                    _logger.LogWarning("Task with ID {TaskId} not found for deletion", id);
                    return NotFound("Task does not exist");
                }

                var result = await _Service.DeleteTask(id);
                if (!result)
                {
                    _logger.LogError("Failed to delete task with ID {TaskId}", id);
                    return BadRequest("Failed to delete task.");
                }
                _logger.LogInformation("Deleted task with ID {TaskId} successfully", id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    _logger.LogError(ex, "Failed to delete task with ID {TaskId}: {Message}", id, ex.Message);
                    return Conflict(ex.Message);
                }
                _logger.LogError(ex, "Failed to delete task with ID {TaskId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting task with ID {TaskId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}