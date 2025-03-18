using Npgsql;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

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
            var incident_historys = new List<IncidentHistoryResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM incident_history";

                using (var cmd = new NpgsqlCommand(sql, _connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var incident_history = new IncidentHistoryResponse
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
                        incident_historys.Add(incident_history);
                    }
                }
                await _connection.CloseAsync();
                return incident_historys;
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
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IncidentHistoryResponse?> CreateIncidentHistory(IncidentHistoryRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO incident_history 
                (incident_id, task_id, changed_by, old_status,new_status,change_description,changed_at)
                VALUES ( @incident_id, @task_id, @changed_by, @old_status,@new_status,@change_description,@changed_at)
                RETURNING category_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@incident_id", entity.incident_id);
                        cmd.Parameters.AddWithValue("@task_id", entity.task_id);
                        cmd.Parameters.AddWithValue("@changed_by", entity.changed_by);
                        cmd.Parameters.AddWithValue("@old_status", entity.old_status);
                        cmd.Parameters.AddWithValue("@new_status", entity.old_status);
                        cmd.Parameters.AddWithValue("@change_description", entity.old_status);
                        cmd.Parameters.AddWithValue("@changed_at", entity.old_status);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetIncidentHistoryById(newId);
                    }
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IncidentHistoryResponse?> UpdateIncidentHistory(int id, IncidentHistoryRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE incident_history SET
                    incident_id  = @incident_id,
                    task_id = @task_id,
                    changed_by = @changed_by,
                    old_status = @old_status,
                    new_status = @new_status,
                    change_description = @change_description,
                    changed_at = @changed_at,
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
                        cmd.Parameters.AddWithValue("@new_status", entity.old_status);
                        cmd.Parameters.AddWithValue("@change_description", entity.old_status);
                        cmd.Parameters.AddWithValue("@changed_at", entity.old_status);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetIncidentHistoryById(id);
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

        public async Task<bool> DeleteIncidentHistory(int id)
        {
            using(var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM incident_history WHERE incident_history_id = @id";

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
