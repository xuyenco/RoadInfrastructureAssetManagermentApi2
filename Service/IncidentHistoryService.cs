using Npgsql;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Road_Infrastructure_Asset_Management.Service
{
    public class IncidentHistoryService : IIncidentHistoryService
    {
        private readonly string _connectionString;

        public IncidentHistoryService(string connection)
        {
            _connectionString = connection;
        }

        public async Task<IEnumerable<IncidentHistoryResponse>> GetAllIncidentHistory()
        {
            var incidentHistories = new List<IncidentHistoryResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM incident_history";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var incidentHistory = new IncidentHistoryResponse
                            {
                                history_id = reader.GetInt32(reader.GetOrdinal("history_id")),
                                incident_id = reader.GetInt32(reader.GetOrdinal("incident_id")),
                                task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                changed_by = reader.GetInt32(reader.GetOrdinal("changed_by")),
                                old_status = reader.GetString(reader.GetOrdinal("old_status")),
                                new_status = reader.GetString(reader.GetOrdinal("new_status")),
                                change_description = reader.GetString(reader.GetOrdinal("change_description")),
                                changed_at = reader.GetDateTime(reader.GetOrdinal("changed_at"))
                            };
                            incidentHistories.Add(incidentHistory);
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to retrieve incident history from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
                return incidentHistories;
            }
        }

        public async Task<IncidentHistoryResponse?> GetIncidentHistoryById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM incident_history WHERE history_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new IncidentHistoryResponse
                                {
                                    history_id = reader.GetInt32(reader.GetOrdinal("history_id")),
                                    incident_id = reader.GetInt32(reader.GetOrdinal("incident_id")),
                                    task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                    changed_by = reader.GetInt32(reader.GetOrdinal("changed_by")),
                                    old_status = reader.GetString(reader.GetOrdinal("old_status")),
                                    new_status = reader.GetString(reader.GetOrdinal("new_status")),
                                    change_description = reader.GetString(reader.GetOrdinal("change_description")),
                                    changed_at = reader.GetDateTime(reader.GetOrdinal("changed_at"))
                                };
                            }
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException($"Failed to retrieve incident history with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<IncidentHistoryResponse>> GetIncidentHistoryByIncidentID(int id)
        {
            var incidentHistories = new List<IncidentHistoryResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM incident_history WHERE incident_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var incidentHistory = new IncidentHistoryResponse
                                {
                                    history_id = reader.GetInt32(reader.GetOrdinal("history_id")),
                                    incident_id = reader.GetInt32(reader.GetOrdinal("incident_id")),
                                    task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                    changed_by = reader.GetInt32(reader.GetOrdinal("changed_by")),
                                    old_status = reader.GetString(reader.GetOrdinal("old_status")),
                                    new_status = reader.GetString(reader.GetOrdinal("new_status")),
                                    change_description = reader.GetString(reader.GetOrdinal("change_description")),
                                    changed_at = reader.GetDateTime(reader.GetOrdinal("changed_at"))
                                };
                                incidentHistories.Add(incidentHistory);
                            }
                        }

                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to retrieve incident history from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
                return incidentHistories;

            }
        }

        public async Task<IncidentHistoryResponse?> CreateIncidentHistory(IncidentHistoryRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO incident_history 
                (incident_id, task_id, changed_by, old_status, new_status, change_description)
                VALUES (@incident_id, @task_id, @changed_by, @old_status, @new_status, @change_description)
                RETURNING history_id"; // Sửa từ category_id thành history_id

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@incident_id", entity.incident_id);
                        cmd.Parameters.AddWithValue("@task_id", entity.task_id);
                        cmd.Parameters.AddWithValue("@changed_by", entity.changed_by);
                        cmd.Parameters.AddWithValue("@old_status", entity.old_status);
                        cmd.Parameters.AddWithValue("@new_status", entity.new_status); // Sửa từ old_status thành new_status
                        cmd.Parameters.AddWithValue("@change_description", entity.change_description ?? (object)DBNull.Value); // Sửa và cho phép NULL
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetIncidentHistoryById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        if (ex.Message.Contains("incident_id"))
                            throw new InvalidOperationException($"Incident ID {entity.incident_id} does not exist.", ex);
                        if (ex.Message.Contains("task_id"))
                            throw new InvalidOperationException($"Task ID {entity.task_id} does not exist.", ex);
                        if (ex.Message.Contains("changed_by"))
                            throw new InvalidOperationException($"User ID {entity.changed_by} does not exist.", ex);
                    }
                    throw new InvalidOperationException("Failed to create incident history.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IncidentHistoryResponse?> UpdateIncidentHistory(int id, IncidentHistoryRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE incident_history SET
                    incident_id = @incident_id,
                    task_id = @task_id,
                    changed_by = @changed_by,
                    old_status = @old_status,
                    new_status = @new_status,
                    change_description = @change_description
                WHERE history_id = @id"; 

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@incident_id", entity.incident_id);
                        cmd.Parameters.AddWithValue("@task_id", entity.task_id);
                        cmd.Parameters.AddWithValue("@changed_by", entity.changed_by);
                        cmd.Parameters.AddWithValue("@old_status", entity.old_status);
                        cmd.Parameters.AddWithValue("@new_status", entity.new_status); // Sửa từ old_status thành new_status
                        cmd.Parameters.AddWithValue("@change_description", entity.change_description ?? (object)DBNull.Value); // Sửa và cho phép NULL
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetIncidentHistoryById(id);
                        }
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        if (ex.Message.Contains("incident_id"))
                            throw new InvalidOperationException($"Incident ID {entity.incident_id} does not exist.", ex);
                        if (ex.Message.Contains("task_id"))
                            throw new InvalidOperationException($"Task ID {entity.task_id} does not exist.", ex);
                        if (ex.Message.Contains("changed_by"))
                            throw new InvalidOperationException($"User ID {entity.changed_by} does not exist.", ex);
                    }
                    throw new InvalidOperationException($"Failed to update incident history with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> DeleteIncidentHistory(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM incident_history WHERE history_id = @id"; // Sửa từ incident_history_id thành history_id

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
                    throw new InvalidOperationException($"Failed to delete incident history with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        // Helper method
        private void ValidateRequest(IncidentHistoryRequest entity)
        {
            if (entity.incident_id <= 0)
            {
                throw new ArgumentException("Incident ID must be a positive integer.");
            }
            if (entity.task_id <= 0)
            {
                throw new ArgumentException("Task ID must be a positive integer.");
            }
            if (entity.changed_by <= 0)
            {
                throw new ArgumentException("Changed by user ID must be a positive integer.");
            }
            if (string.IsNullOrWhiteSpace(entity.old_status))
            {
                throw new ArgumentException("Old status cannot be empty.");
            }
            if (string.IsNullOrWhiteSpace(entity.new_status))
            {
                throw new ArgumentException("New status cannot be empty.");
            }
            
        }
    }
}