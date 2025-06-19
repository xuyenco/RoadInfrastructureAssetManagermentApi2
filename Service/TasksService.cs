using Microsoft.Extensions.Logging;
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;
using Newtonsoft.Json;
using Road_Infrastructure_Asset_Management_2.Model.Geometry;
using System.Text;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class TasksService : ITasksService
    {
        private readonly string _connectionString;
        private readonly ILogger<TasksService> _logger;

        public TasksService(string connectionString, ILogger<TasksService> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<IEnumerable<TasksResponse>> GetAllTasks()
        {
            var tasks = new List<TasksResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT task_id, task_type, work_volume, status, address, ST_AsGeoJSON(geometry) as geometry, start_date, end_date, execution_unit_id, supervisor_id, method_summary, main_result, description, created_at FROM tasks";

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
                                task_type = reader.IsDBNull(reader.GetOrdinal("task_type")) ? null : reader.GetString(reader.GetOrdinal("task_type")),
                                work_volume = reader.IsDBNull(reader.GetOrdinal("work_volume")) ? null : reader.GetString(reader.GetOrdinal("work_volume")),
                                status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetString(reader.GetOrdinal("status")),
                                address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                                geometry = ParseGeoJson(reader.GetString(reader.GetOrdinal("geometry")), "geometry"),
                                start_date = reader.IsDBNull(reader.GetOrdinal("start_date")) ? null : reader.GetDateTime(reader.GetOrdinal("start_date")),
                                end_date = reader.IsDBNull(reader.GetOrdinal("end_date")) ? null : reader.GetDateTime(reader.GetOrdinal("end_date")),
                                execution_unit_id = reader.IsDBNull(reader.GetOrdinal("execution_unit_id")) ? null : reader.GetInt32(reader.GetOrdinal("execution_unit_id")),
                                supervisor_id = reader.IsDBNull(reader.GetOrdinal("supervisor_id")) ? null : reader.GetInt32(reader.GetOrdinal("supervisor_id")),
                                method_summary = reader.IsDBNull(reader.GetOrdinal("method_summary")) ? null : reader.GetString(reader.GetOrdinal("method_summary")),
                                main_result = reader.IsDBNull(reader.GetOrdinal("main_result")) ? null : reader.GetString(reader.GetOrdinal("main_result")),
                                description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
                            };
                            tasks.Add(task);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} tasks successfully", tasks.Count);
                    return tasks;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve tasks from database");
                    throw new InvalidOperationException("Failed to retrieve tasks from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<TasksResponse?> GetTaskById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT task_id, task_type, work_volume, status, address, ST_AsGeoJSON(geometry) as geometry, start_date, end_date, execution_unit_id, supervisor_id, method_summary, main_result, description, created_at FROM tasks WHERE task_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var task = new TasksResponse
                                {
                                    task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                    task_type = reader.IsDBNull(reader.GetOrdinal("task_type")) ? null : reader.GetString(reader.GetOrdinal("task_type")),
                                    work_volume = reader.IsDBNull(reader.GetOrdinal("work_volume")) ? null : reader.GetString(reader.GetOrdinal("work_volume")),
                                    status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetString(reader.GetOrdinal("status")),
                                    address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                                    geometry = ParseGeoJson(reader.GetString(reader.GetOrdinal("geometry")), "geometry"),
                                    start_date = reader.IsDBNull(reader.GetOrdinal("start_date")) ? null : reader.GetDateTime(reader.GetOrdinal("start_date")),
                                    end_date = reader.IsDBNull(reader.GetOrdinal("end_date")) ? null : reader.GetDateTime(reader.GetOrdinal("end_date")),
                                    execution_unit_id = reader.IsDBNull(reader.GetOrdinal("execution_unit_id")) ? null : reader.GetInt32(reader.GetOrdinal("execution_unit_id")),
                                    supervisor_id = reader.IsDBNull(reader.GetOrdinal("supervisor_id")) ? null : reader.GetInt32(reader.GetOrdinal("supervisor_id")),
                                    method_summary = reader.IsDBNull(reader.GetOrdinal("method_summary")) ? null : reader.GetString(reader.GetOrdinal("method_summary")),
                                    main_result = reader.IsDBNull(reader.GetOrdinal("main_result")) ? null : reader.GetString(reader.GetOrdinal("main_result")),
                                    description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                _logger.LogInformation("Retrieved task with ID {TaskId} successfully", id);
                                return task;
                            }
                            _logger.LogWarning("Task with ID {TaskId} not found", id);
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve task with ID {TaskId}", id);
                    throw new InvalidOperationException($"Failed to retrieve task with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<(IEnumerable<TasksResponse> Tasks, int TotalCount)> GetTasksPagination(int page, int pageSize, string searchTerm, int searchField)
        {
            var tasks = new List<TasksResponse>();
            int totalCount = 0;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Xây dựng câu lệnh truy vấn
                var sqlBuilder = new StringBuilder(@"SELECT task_id, task_type, work_volume, status, address, 
                                            ST_AsGeoJSON(geometry) as geometry, start_date, end_date, 
                                            execution_unit_id, supervisor_id, method_summary, main_result, 
                                            description, created_at 
                                            FROM tasks");
                var countSql = "SELECT COUNT(*) FROM tasks";
                var parameters = new List<NpgsqlParameter>();

                // Thêm điều kiện tìm kiếm nếu có
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = $"%{searchTerm.ToLower()}%"; // Chuẩn bị cho ILIKE
                    string condition = searchField switch
                    {
                        0 => "CAST(task_id AS TEXT) ILIKE @searchTerm",           // ID nhiệm vụ
                        1 => "LOWER(task_type) ILIKE @searchTerm",               // Loại nhiệm vụ
                        2 => "LOWER(work_volume) ILIKE @searchTerm",             // Khối lượng công việc
                        3 => "LOWER(status) ILIKE @searchTerm",                  // Trạng thái
                        4 => "LOWER(address) ILIKE @searchTerm",                 // Địa chỉ
                        5 => "TO_CHAR(start_date, 'DD/MM/YYYY HH24:MI') ILIKE @searchTerm", // Ngày bắt đầu
                        6 => "TO_CHAR(end_date, 'DD/MM/YYYY HH24:MI') ILIKE @searchTerm",   // Ngày kết thúc
                        7 => "LOWER(method_summary) ILIKE @searchTerm",          // Tóm tắt phương pháp
                        8 => "LOWER(main_result) ILIKE @searchTerm",             // Kết quả chính
                        9 => "LOWER(description) ILIKE @searchTerm",             // Mô tả
                        10 => "TO_CHAR(created_at, 'HH24:MI DD/MM/YYYY') ILIKE @searchTerm", // Ngày tạo
                        _ => null
                    };

                    if (condition != null)
                    {
                        sqlBuilder.Append(" WHERE ");
                        sqlBuilder.Append(condition);
                        countSql += $" WHERE {condition}";
                        parameters.Add(new NpgsqlParameter("@searchTerm", searchTerm));
                    }
                }

                // Thêm phân trang
                sqlBuilder.Append(" ORDER BY task_id");
                sqlBuilder.Append(" OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY");
                parameters.Add(new NpgsqlParameter("@offset", (page - 1) * pageSize));
                parameters.Add(new NpgsqlParameter("@pageSize", pageSize));

                try
                {
                    // Đếm tổng số bản ghi
                    using (var countCmd = new NpgsqlCommand(countSql, connection))
                    {
                        foreach (var param in parameters)
                        {
                            countCmd.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
                        }
                        totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                    }

                    // Lấy danh sách nhiệm vụ
                    using (var cmd = new NpgsqlCommand(sqlBuilder.ToString(), connection))
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
                        }
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var task = new TasksResponse
                                {
                                    task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                    task_type = reader.IsDBNull(reader.GetOrdinal("task_type")) ? null : reader.GetString(reader.GetOrdinal("task_type")),
                                    work_volume = reader.IsDBNull(reader.GetOrdinal("work_volume")) ? null : reader.GetString(reader.GetOrdinal("work_volume")),
                                    status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetString(reader.GetOrdinal("status")),
                                    address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                                    geometry = ParseGeoJson(reader.GetString(reader.GetOrdinal("geometry")), "geometry"),
                                    start_date = reader.IsDBNull(reader.GetOrdinal("start_date")) ? null : reader.GetDateTime(reader.GetOrdinal("start_date")),
                                    end_date = reader.IsDBNull(reader.GetOrdinal("end_date")) ? null : reader.GetDateTime(reader.GetOrdinal("end_date")),
                                    execution_unit_id = reader.IsDBNull(reader.GetOrdinal("execution_unit_id")) ? null : reader.GetInt32(reader.GetOrdinal("execution_unit_id")),
                                    supervisor_id = reader.IsDBNull(reader.GetOrdinal("supervisor_id")) ? null : reader.GetInt32(reader.GetOrdinal("supervisor_id")),
                                    method_summary = reader.IsDBNull(reader.GetOrdinal("method_summary")) ? null : reader.GetString(reader.GetOrdinal("method_summary")),
                                    main_result = reader.IsDBNull(reader.GetOrdinal("main_result")) ? null : reader.GetString(reader.GetOrdinal("main_result")),
                                    description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                tasks.Add(task);
                            }
                        }
                    }

                    _logger.LogInformation("Retrieved {Count} tasks for page {Page} with total count {TotalCount}", tasks.Count, page, totalCount);
                    return (tasks, totalCount);
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve tasks with pagination and search");
                    throw new InvalidOperationException("Failed to retrieve tasks from database.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
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
                (task_type, work_volume, status, address, geometry, start_date, end_date, execution_unit_id, supervisor_id, method_summary, main_result, description)
                VALUES (@task_type, @work_volume, @status, @address, ST_SetSRID(ST_GeomFromGeoJSON(@geometry), 3405), @start_date, @end_date, @execution_unit_id, @supervisor_id, @method_summary, @main_result, @description)
                RETURNING task_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@task_type", entity.task_type);
                        cmd.Parameters.AddWithValue("@work_volume", (object)entity.work_volume ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@status", entity.status);
                        cmd.Parameters.AddWithValue("@address", (object)entity.address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@geometry", JsonConvert.SerializeObject(entity.geometry));
                        cmd.Parameters.AddWithValue("@start_date", (object)entity.start_date ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@end_date", (object)entity.end_date ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@execution_unit_id", (object)entity.execution_unit_id ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@supervisor_id", (object)entity.supervisor_id ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@method_summary", (object)entity.method_summary ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@main_result", (object)entity.main_result ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@description", (object)entity.description ?? DBNull.Value);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        _logger.LogInformation("Created task with ID {TaskId} successfully", newId);
                        return await GetTaskById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        if (ex.Message.Contains("execution_unit_id"))
                        {
                            _logger.LogError(ex, "Failed to create task: Invalid execution_unit_id {ExecutionUnitId}", entity.execution_unit_id);
                            throw new InvalidOperationException($"User ID {entity.execution_unit_id} does not exist.", ex);
                        }
                        if (ex.Message.Contains("supervisor_id"))
                        {
                            _logger.LogError(ex, "Failed to create task: Invalid supervisor_id {SupervisorId}", entity.supervisor_id);
                            throw new InvalidOperationException($"User ID {entity.supervisor_id} does not exist.", ex);
                        }
                    }
                    else if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        _logger.LogError(ex, "Failed to create task: Invalid GeoJSON format");
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
                    }
                    _logger.LogError(ex, "Failed to create task");
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
            //ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE tasks SET
                    task_type = @task_type,
                    work_volume = @work_volume,
                    status = @status,
                    address = @address,
                    geometry = ST_SetSRID(ST_GeomFromGeoJSON(@geometry), 3405),
                    start_date = @start_date,
                    end_date = @end_date,
                    execution_unit_id = @execution_unit_id,
                    supervisor_id = @supervisor_id,
                    method_summary = @method_summary,
                    main_result = @main_result,
                    description = @description
                WHERE task_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@task_type", entity.task_type);
                        cmd.Parameters.AddWithValue("@work_volume", entity.work_volume);
                        cmd.Parameters.AddWithValue("@status", entity.status);
                        cmd.Parameters.AddWithValue("@address", (object)entity.address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@geometry", JsonConvert.SerializeObject(entity.geometry));
                        cmd.Parameters.AddWithValue("@start_date", (object)entity.start_date ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@end_date", (object)entity.end_date ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@execution_unit_id", (object)entity.execution_unit_id ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@supervisor_id", (object)entity.supervisor_id ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@method_summary", (object)entity.method_summary ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@main_result", (object)entity.main_result ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@description", (object)entity.description ?? DBNull.Value);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Updated task with ID {TaskId} successfully", id);
                            return await GetTaskById(id);
                        }
                        _logger.LogWarning("Task with ID {TaskId} not found for update", id);
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        if (ex.Message.Contains("execution_unit_id"))
                        {
                            _logger.LogError(ex, "Failed to update task with ID {TaskId}: Invalid execution_unit_id {ExecutionUnitId}", id, entity.execution_unit_id);
                            throw new InvalidOperationException($"User ID {entity.execution_unit_id} does not exist.", ex);
                        }
                        if (ex.Message.Contains("supervisor_id"))
                        {
                            _logger.LogError(ex, "Failed to update task with ID {TaskId}: Invalid supervisor_id {SupervisorId}", id, entity.supervisor_id);
                            throw new InvalidOperationException($"User ID {entity.supervisor_id} does not exist.", ex);
                        }
                    }
                    else if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        _logger.LogError(ex, "Failed to update task with ID {TaskId}: Invalid GeoJSON format", id);
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
                    }
                    _logger.LogError(ex, "Failed to update task with ID {TaskId}", id);
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
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Deleted task with ID {TaskId} successfully", id);
                            return true;
                        }
                        _logger.LogWarning("Task with ID {TaskId} not found for deletion", id);
                        return false;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to delete task with ID {TaskId}: Task is referenced by other records", id);
                        throw new InvalidOperationException($"Cannot delete task with ID {id} because it is referenced by other records (e.g., costs).", ex);
                    }
                    _logger.LogError(ex, "Failed to delete task with ID {TaskId}", id);
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
            if (string.IsNullOrWhiteSpace(entity.task_type))
            {
                _logger.LogWarning("Validation failed: Task type cannot be empty");
                throw new ArgumentException("Task type cannot be empty.");
            }
            if (string.IsNullOrWhiteSpace(entity.status))
            {
                _logger.LogWarning("Validation failed: Status cannot be empty");
                throw new ArgumentException("Status cannot be empty.");
            }
            if (entity.geometry == null || string.IsNullOrEmpty(entity.geometry.type))
            {
                _logger.LogWarning("Validation failed: Geometry cannot be null or invalid");
                throw new ArgumentException("Geometry cannot be null or invalid.");
            }
            try
            {
                JsonConvert.SerializeObject(entity.geometry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Validation failed: Invalid GeoJSON format for geometry");
                throw new ArgumentException("Invalid GeoJSON format for geometry.", ex);
            }
            if (entity.execution_unit_id.HasValue && entity.execution_unit_id <= 0)
            {
                _logger.LogWarning("Validation failed: Execution unit ID must be a positive integer");
                throw new ArgumentException("Execution unit ID must be a positive integer.");
            }
            if (entity.supervisor_id.HasValue && entity.supervisor_id <= 0)
            {
                _logger.LogWarning("Validation failed: Supervisor ID must be a positive integer");
                throw new ArgumentException("Supervisor ID must be a positive integer.");
            }
        }

        private GeoJsonGeometry ParseGeoJson(string json, string fieldName)
        {
            try
            {
                return JsonConvert.DeserializeObject<GeoJsonGeometry>(json);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse GeoJSON for {FieldName}", fieldName);
                throw new InvalidOperationException($"Invalid GeoJSON format for {fieldName}.", ex);
            }
        }
    }
}