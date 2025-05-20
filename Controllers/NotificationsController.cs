using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management_2.Controllers;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationsService _Service;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationsService Service, ILogger<NotificationsController> logger)
        {
            _Service = Service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllNotifications()
        {
            try
            {
                _logger.LogInformation("Received request to get all notifications");
                var notifications = await _Service.GetAllNotifications();
                _logger.LogInformation("Returned {Count} notifications", notifications.Count());
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all notifications");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("userid/{id}")]
        public async Task<ActionResult> GetNotificationById(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get all notifications");
                var notifications = await _Service.GetAllNotificationsByUserId(id);
                _logger.LogInformation("Returned {Count} notifications", notifications.Count());
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all notifications");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetNotificationsById(int id)
        {
            try
            {
                _logger.LogInformation("Received request to get notification with ID {NotificationId}", id);
                var notification = await _Service.GetNotificationById(id);
                if (notification == null)
                {
                    _logger.LogWarning("Notification with ID {NotificationId} not found", id);
                    return NotFound("Notification does not exist");
                }
                _logger.LogInformation("Returned notification with ID {NotificationId}", id);
                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notification with ID {NotificationId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateNotifications([FromBody] NotificationRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model for creating notification");
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Received request to create notification");
                var notification = await _Service.CreateNotification(request);
                if (notification == null)
                {
                    _logger.LogError("Failed to create notification");
                    return BadRequest("Failed to create notification.");
                }
                _logger.LogInformation("Created notification with ID {NotificationId} successfully", notification.notification_id);
                return CreatedAtAction(nameof(GetNotificationsById), new { id = notification.notification_id }, notification);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for creating notification: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to create notification: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating notification");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateNotifications(int id, [FromBody] NotificationRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model for updating notification with ID {NotificationId}", id);
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Received request to update notification with ID {NotificationId}", id);
                var existingNotification = await _Service.GetNotificationById(id);
                if (existingNotification == null)
                {
                    _logger.LogWarning("Notification with ID {NotificationId} not found for update", id);
                    return NotFound("Notification does not exist");
                }

                var updatedNotification = await _Service.UpdateNotification(id, request);
                if (updatedNotification == null)
                {
                    _logger.LogError("Failed to update notification with ID {NotificationId}", id);
                    return BadRequest("Failed to update notification.");
                }
                _logger.LogInformation("Updated notification with ID {NotificationId} successfully", id);
                return Ok(updatedNotification);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for updating notification with ID {NotificationId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to update notification with ID {NotificationId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating notification with ID {NotificationId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotifications(int id)
        {
            try
            {
                _logger.LogInformation("Received request to delete notification with ID {NotificationId}", id);
                var existingNotification = await _Service.GetNotificationById(id);
                if (existingNotification == null)
                {
                    _logger.LogWarning("Notification with ID {NotificationId} not found for deletion", id);
                    return NotFound("Notification does not exist");
                }

                var result = await _Service.DeleteNotification(id);
                if (!result)
                {
                    _logger.LogError("Failed to delete notification with ID {NotificationId}", id);
                    return BadRequest("Failed to delete notification.");
                }
                _logger.LogInformation("Deleted notification with ID {NotificationId} successfully", id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("referenced by other records"))
                {
                    _logger.LogError(ex, "Failed to delete notification with ID {NotificationId}: {Message}", id, ex.Message);
                    return Conflict(ex.Message);
                }
                _logger.LogError(ex, "Failed to delete notification with ID {NotificationId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting notification with ID {NotificationId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
