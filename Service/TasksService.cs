﻿using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Road_Infrastructure_Asset_Management_2.Model.Geometry;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class TasksService : ITasksService
    {
        private readonly string _connectionString;

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
                var sql = "SELECT task_id, task_type, work_volume, status, address, ST_AsGeoJSON(geometry) as geometry, start_date, end_date, execution_unit_id, supervisor_id, method_summary, main_result, created_at FROM tasks";

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
                                created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
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
                var sql = "SELECT task_id, task_type, work_volume, status, address, ST_AsGeoJSON(geometry) as geometry, start_date, end_date, execution_unit_id, supervisor_id, method_summary, main_result, created_at FROM tasks WHERE task_id = @id";

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
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
                                   
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
                (task_type, work_volume, status, address, geometry, start_date, end_date, execution_unit_id, supervisor_id, method_summary, main_result)
                VALUES (@task_type, @work_volume, @status, @address, ST_SetSRID(ST_GeomFromGeoJSON(@geometry), 3405), @start_date, @end_date, @execution_unit_id, @supervisor_id, @method_summary, @main_result)
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
                        cmd.Parameters.AddWithValue("@method_summary", (object)entity.method_summary ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@main_result", (object)entity.main_result ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@supervisor_id", (object)entity.supervisor_id ?? DBNull.Value);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetTaskById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        if (ex.Message.Contains("execution_unit_id"))
                            throw new InvalidOperationException($"User ID {entity.execution_unit_id} does not exist.", ex);
                        if (ex.Message.Contains("supervisor_id"))
                            throw new InvalidOperationException($"User ID {entity.supervisor_id} does not exist.", ex);
                    }
                    else if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
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
                    main_result = @main_result
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
                        if (ex.Message.Contains("execution_unit_id"))
                            throw new InvalidOperationException($"User ID {entity.execution_unit_id} does not exist.", ex);
                        if (ex.Message.Contains("supervisor_id"))
                            throw new InvalidOperationException($"User ID {entity.supervisor_id} does not exist.", ex);
                    }
                    else if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
                    }
                    throw new InvalidOperationException($"Failed to update task with ID {id}: {ex}");
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
                        throw new InvalidOperationException($"Cannot delete task with ID {id} because it is referenced by other records (e.g., costs).", ex);
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
            if (string.IsNullOrWhiteSpace(entity.task_type))
            {
                throw new ArgumentException("Task type cannot be empty.");
            }
            if (string.IsNullOrWhiteSpace(entity.status))
            {
                throw new ArgumentException("Status cannot be empty.");
            }
            if (entity.geometry == null || string.IsNullOrEmpty(entity.geometry.type))
            {
                throw new ArgumentException("Geometry cannot be null or invalid.");
            }
            try
            {
                JsonConvert.SerializeObject(entity.geometry);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid GeoJSON format for geometry.", ex);
            }
            if (entity.execution_unit_id.HasValue && entity.execution_unit_id <= 0)
            {
                throw new ArgumentException("Execution unit ID must be a positive integer.");
            }
            if (entity.supervisor_id.HasValue && entity.supervisor_id <= 0)
            {
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
                throw new InvalidOperationException($"Invalid GeoJSON format for {fieldName}.", ex);
            }
        }
    }
}