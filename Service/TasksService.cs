using Npgsql;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Road_Infrastructure_Asset_Management.Service
{
    public class TasksService : ITasksService
    {
        private readonly string _connectionString;
        private static readonly string[] ValidPriorities = { "low", "medium", "high" };
        private static readonly string[] ValidStatuses = { "pending", "in_progress", "completed" };

        public TasksService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<TasksResponse>> GetAllTasks()
        {
            var tasks = new List<TasksResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM tasks";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var task = new TasksResponse
                            {
                                task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                assigned_to = reader.GetInt32(reader.GetOrdinal("assigned_to")),
                                task_type = reader.GetString(reader.GetOrdinal("task_type")),
                                description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                priority = reader.GetString(reader.GetOrdinal("priority")),
                                status = reader.GetString(reader.GetOrdinal("status")),
                                due_date = reader.GetDateTime(reader.GetOrdinal("due_date")),
                                created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                updated_at = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                            };
                            tasks.Add(task);
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to retrieve tasks from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
                return tasks;
            }
        }

        public async Task<TasksResponse?> GetTaskById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM tasks WHERE task_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new TasksResponse
                                {
                                    task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                    asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                    assigned_to = reader.GetInt32(reader.GetOrdinal("assigned_to")),
                                    task_type = reader.GetString(reader.GetOrdinal("task_type")),
                                    description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                    priority = reader.GetString(reader.GetOrdinal("priority")),
                                    status = reader.GetString(reader.GetOrdinal("status")),
                                    due_date = reader.GetDateTime(reader.GetOrdinal("due_date")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    updated_at = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                                };
                            }
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException($"Failed to retrieve task with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<TasksResponse?> CreateTask(TasksRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO tasks 
                (asset_id, assigned_to, task_type, description, priority, status, due_date)
                VALUES (@asset_id, @assigned_to, @task_type, @description, @priority, @status, @due_date)
                RETURNING task_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@asset_id", entity.asset_id);
                        cmd.Parameters.AddWithValue("@assigned_to", entity.assigned_to);
                        cmd.Parameters.AddWithValue("@task_type", entity.task_type);
                        cmd.Parameters.AddWithValue("@description", entity.description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@priority", entity.priority);
                        cmd.Parameters.AddWithValue("@status", entity.status);
                        cmd.Parameters.AddWithValue("@due_date", entity.due_date);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetTaskById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        if (ex.Message.Contains("asset_id"))
                            throw new InvalidOperationException($"Asset ID {entity.asset_id} does not exist.", ex);
                        if (ex.Message.Contains("assigned_to"))
                            throw new InvalidOperationException($"User ID {entity.assigned_to} does not exist.", ex);
                    }
                    else if (ex.SqlState == "23514") // Check constraint violation
                    {
                        if (ex.Message.Contains("priority"))
                            throw new InvalidOperationException($"Priority must be one of: {string.Join(", ", ValidPriorities)}.", ex);
                        if (ex.Message.Contains("status"))
                            throw new InvalidOperationException($"Status must be one of: {string.Join(", ", ValidStatuses)}.", ex);
                    }
                    throw new InvalidOperationException("Failed to create task.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<TasksResponse?> UpdateTask(int id, TasksRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE tasks SET
                    asset_id = @asset_id,
                    assigned_to = @assigned_to,
                    task_type = @task_type,
                    description = @description,
                    priority = @priority,
                    status = @status,
                    due_date = @due_date,
                    updated_at = @updated_at
                WHERE task_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@asset_id", entity.asset_id);
                        cmd.Parameters.AddWithValue("@assigned_to", entity.assigned_to);
                        cmd.Parameters.AddWithValue("@task_type", entity.task_type);
                        cmd.Parameters.AddWithValue("@description", entity.description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@priority", entity.priority);
                        cmd.Parameters.AddWithValue("@status", entity.status);
                        cmd.Parameters.AddWithValue("@due_date", entity.due_date);
                        cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow); // Cập nhật thời gian

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetTaskById(id);
                        }
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        if (ex.Message.Contains("asset_id"))
                            throw new InvalidOperationException($"Asset ID {entity.asset_id} does not exist.", ex);
                        if (ex.Message.Contains("assigned_to"))
                            throw new InvalidOperationException($"User ID {entity.assigned_to} does not exist.", ex);
                    }
                    else if (ex.SqlState == "23514") // Check constraint violation
                    {
                        if (ex.Message.Contains("priority"))
                            throw new InvalidOperationException($"Priority must be one of: {string.Join(", ", ValidPriorities)}.", ex);
                        if (ex.Message.Contains("status"))
                            throw new InvalidOperationException($"Status must be one of: {string.Join(", ", ValidStatuses)}.", ex);
                    }
                    throw new InvalidOperationException($"Failed to update task with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> DeleteTask(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM tasks WHERE task_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        return affectedRows > 0;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Cannot delete task with ID {id} because it is referenced by other records (e.g., costs or incident_history).", ex);
                    }
                    throw new InvalidOperationException($"Failed to delete task with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        // Helper method
        private void ValidateRequest(TasksRequest entity)
        {
            if (entity.asset_id <= 0)
            {
                throw new ArgumentException("Asset ID must be a positive integer.");
            }
            if (entity.assigned_to <= 0)
            {
                throw new ArgumentException("Assigned to user ID must be a positive integer.");
            }
            if (string.IsNullOrWhiteSpace(entity.task_type))
            {
                throw new ArgumentException("Task type cannot be empty.");
            }
            if (!ValidPriorities.Contains(entity.priority))
            {
                throw new ArgumentException($"Priority must be one of: {string.Join(", ", ValidPriorities)}.");
            }
            if (!ValidStatuses.Contains(entity.status))
            {
                throw new ArgumentException($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
            }
            if (entity.due_date == default(DateTime))
            {
                throw new ArgumentException("Due date must be provided.");
            }
        }
    }
}