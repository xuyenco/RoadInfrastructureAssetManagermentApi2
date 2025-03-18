using Npgsql;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace Road_Infrastructure_Asset_Management.Service
{
    public class TasksService : ITasksService
    {
        private readonly string _connectionString;

        public TasksService(String connectionString)
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
                            description = reader.GetString(reader.GetOrdinal("Description")),
                            priority = reader.GetString(reader.GetOrdinal("priority")),
                            status = reader.GetString(reader.GetOrdinal("status")),
                            due_date = reader.GetDateTime(reader.GetOrdinal("due_date")),
                            created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                            updated_at = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                        };
                        tasks.Add(task);
                    }
                }
                await _connection.CloseAsync();
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
                                    description = reader.GetString(reader.GetOrdinal("description")),
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
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }
        public async Task<TasksResponse?> CreateTask(TasksRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO tasks 
                (asset_id, assigned_to, task_type, description, priority,status,due_date)
                VALUES (@asset_id, @assigned_to, @task_type, @description, @priority,@status,@due_date)
                RETURNING task_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@asset_id", entity.asset_id);
                        cmd.Parameters.AddWithValue("@assigned_to", entity.assigned_to);
                        cmd.Parameters.AddWithValue("@task_type", entity.task_type);
                        cmd.Parameters.AddWithValue("@description", entity.description);
                        cmd.Parameters.AddWithValue("@priority", entity.priority);
                        cmd.Parameters.AddWithValue("@status", entity.status);
                        cmd.Parameters.AddWithValue("@due_date", entity.due_date);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetTaskById(newId);
                    }
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<TasksResponse?> UpdateTask(int id, TasksRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE tasks SET
                    asset_id  = @asset_id,
                    assigned_to = @assigned_to,
                    task_type = @task_type,
                    description = @description,
                    priority = @priority,
                    status = @status,
                    due_date = @due_date
                WHERE task_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@asset_id", entity.asset_id);
                        cmd.Parameters.AddWithValue("@assigned_to", entity.assigned_to);
                        cmd.Parameters.AddWithValue("@task_type", entity.task_type);
                        cmd.Parameters.AddWithValue("@description", entity.description);
                        cmd.Parameters.AddWithValue("@priority", entity.priority);
                        cmd.Parameters.AddWithValue("@status", entity.status);
                        cmd.Parameters.AddWithValue("@due_date", entity.due_date);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetTaskById(id);
                        }
                        return null;
                    }
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
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }


    }
}
