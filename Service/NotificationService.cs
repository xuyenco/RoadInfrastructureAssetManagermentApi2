using Microsoft.AspNetCore.SignalR;
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Hubs;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;
using Road_Infrastructure_Asset_Management_2.Service;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class NotificationService : INotificationsService
    {
        private readonly string _connectionString;
        private readonly ILogger<NotificationService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(string connectionString, ILogger<NotificationService> logger, IHubContext<NotificationHub> hubContext)
        {
            _connectionString = connectionString;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<IEnumerable<NotificationResponse>> GetAllNotifications()
        {
            var notifications = new List<NotificationResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM notifications";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var notification = new NotificationResponse
                            {
                                notification_id = reader.GetInt32(reader.GetOrdinal("notification_id")),
                                user_id = reader.GetInt32(reader.GetOrdinal("user_id")),
                                task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                message = reader.GetString(reader.GetOrdinal("message")),
                                is_read = reader.GetBoolean(reader.GetOrdinal("is_read")),
                                created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                notification_type = reader.GetString(reader.GetOrdinal("notification_type")),
                                
                            };
                            notifications.Add(notification);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} notification successfully", notifications.Count);
                    return notifications;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve notifications from database");
                    throw new InvalidOperationException("Failed to retrieve notifications from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<NotificationResponse>> GetAllNotificationsByUserId(int id)
        {
            var notifications = new List<NotificationResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM notifications WHERE user_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var notification = new NotificationResponse
                                {
                                    notification_id = reader.GetInt32(reader.GetOrdinal("notification_id")),
                                    user_id = reader.GetInt32(reader.GetOrdinal("user_id")),
                                    task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                    message = reader.GetString(reader.GetOrdinal("message")),
                                    is_read = reader.GetBoolean(reader.GetOrdinal("is_read")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    notification_type = reader.GetString(reader.GetOrdinal("notification_type")),

                                };
                                notifications.Add(notification);
                            }
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} notification successfully", notifications.Count);
                    return notifications;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve notifications from database");
                    throw new InvalidOperationException("Failed to retrieve notifications from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<NotificationResponse?> GetNotificationById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM notifications WHERE notification_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var notification = new NotificationResponse
                                {
                                    notification_id = reader.GetInt32(reader.GetOrdinal("notification_id")),
                                    user_id = reader.GetInt32(reader.GetOrdinal("user_id")),
                                    task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                    message = reader.GetString(reader.GetOrdinal("message")),
                                    is_read = reader.GetBoolean(reader.GetOrdinal("is_read")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    notification_type = reader.GetString(reader.GetOrdinal("notification_type")),

                                };
                                _logger.LogInformation("Retrieved notification with ID {NotificationId} successfully", id);
                                return notification;
                            }
                            _logger.LogWarning("Notification with ID {NotificationId} not found", id);
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve notification with ID {NotificationId}", id);
                    throw new InvalidOperationException($"Failed to retrieve notification with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<NotificationResponse?> CreateNotification(NotificationRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO notifications 
                (user_id, task_id, message, is_read, notification_type)
                VALUES (@user_id, @task_id, @message, @is_read, @notification_type)
                RETURNING notification_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@user_id", entity.user_id);
                        cmd.Parameters.AddWithValue("@task_id", entity.task_id);
                        cmd.Parameters.AddWithValue("@message", entity.message);
                        cmd.Parameters.AddWithValue("@is_read", entity.is_read);
                        cmd.Parameters.AddWithValue("@notification_type", entity.notification_type);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        _logger.LogInformation("Created notification with ID {NotificationId} successfully", newId);

                        // Lấy thông báo vừa tạo để gửi qua SignalR
                        var newNotification = await GetNotificationById(newId);
                        if (newNotification != null)
                        {
                            // Gửi thông báo đến client thuộc nhóm userId
                            await _hubContext.Clients.Group(entity.user_id.ToString())
                                .SendAsync("ReceiveNotification", newNotification);
                        }

                        return newNotification;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to create notification: Invalid user ID {userId} or task ID {TaskID}", entity.user_id,entity.task_id);
                        throw new InvalidOperationException($"User ID {entity.user_id} or Task ID {entity.task_id} does not exist.", ex);
                    }
                    _logger.LogError(ex, "Failed to create notification");
                    throw new InvalidOperationException("Failed to create notification.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<NotificationResponse?> UpdateNotification(int id, NotificationRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE notifications SET
                    user_id = @user_id,
                    task_id = @task_id,
                    message = @message,
                    is_read = @is_read,
                    notification_type = @notification_type
                WHERE notification_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@user_id", entity.user_id);
                        cmd.Parameters.AddWithValue("@task_id", entity.task_id);
                        cmd.Parameters.AddWithValue("@message", entity.message);
                        cmd.Parameters.AddWithValue("@is_read", entity.is_read);
                        cmd.Parameters.AddWithValue("@notification_type", entity.notification_type);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Updated notification with ID {NotificationId} successfully", id);
                            return await GetNotificationById(id);
                        }
                        _logger.LogWarning("Notification with ID {NotificationId} not found for update", id);
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to update notification: Invalid user ID {userId} or task ID {TaskID}", entity.user_id, entity.task_id);
                        throw new InvalidOperationException($"User ID {entity.user_id} or Task ID {entity.task_id} does not exist.", ex);
                    }
                    _logger.LogError(ex, "Failed to update notification with ID {NotificationId}", id);
                    throw new InvalidOperationException($"Failed to update notification with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> DeleteNotification(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM notifications WHERE notification_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Deleted notification with ID {NotificationId} successfully", id);
                            return true;
                        }
                        _logger.LogWarning("Notification with ID {NotificationId} not found for deletion", id);
                        return false;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to delete notification with ID {NotificationId}: Referenced by other records", id);
                        throw new InvalidOperationException($"Cannot delete notification with ID {id} because it is referenced by other records.", ex);
                    }
                    _logger.LogError(ex, "Failed to delete notification with ID {NotificationId}", id);
                    throw new InvalidOperationException($"Failed to delete notification with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }
    }
}
