using Newtonsoft.Json;
using Npgsql;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Geometry;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace Road_Infrastructure_Asset_Management.Service
{
    public class IncidentService :IIncidentsService
    {
        private readonly string _connectionString;

        public IncidentService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<IncidentsResponse>> GetAllIncidents()
        {
            var incidents = new List<IncidentsResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT incident_id,asset_id,reported_by,incident_type,description,ST_AsGeoJSON(ST_Transform(location, 4326)) as location, priority,status,reported_at,resolved_at,notes " +
                    "FROM incidents";

                using (var cmd = new NpgsqlCommand(sql, _connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var incident = new IncidentsResponse
                        {
                            incident_id = reader.GetInt32(reader.GetOrdinal("incident_id")),
                            asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                            reported_by = reader.GetInt32(reader.GetOrdinal("reported_by")),
                            incident_type = reader.GetString(reader.GetOrdinal("incident_type")),
                            description = reader.GetString(reader.GetOrdinal("description")),
                            location = JsonConvert.DeserializeObject<GeoJsonGeometry>(reader.GetString(reader.GetOrdinal("location"))),
                            priority = reader.GetString(reader.GetOrdinal("priority")),
                            status = reader.GetString(reader.GetOrdinal("status")),
                            reported_at = reader.GetDateTime(reader.GetOrdinal("reported_at")),
                            resolved_at = reader.GetDateTime(reader.GetOrdinal("resolved_at")),
                            notes = reader.GetString(reader.GetOrdinal("notes")),
                        };
                        incidents.Add(incident);
                    }
                }
                await _connection.CloseAsync();
                return incidents;
            }
        }
        public async Task<IncidentsResponse?> GetIncidentById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT incident_id,asset_id,reported_by,incident_type,description,ST_AsGeoJSON(ST_Transform(location, 4326)) as location, priority,status,reported_at,resolved_at,notes " +
                    "FROM incidents WHERE incident_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new IncidentsResponse
                                {
                                    incident_id = reader.GetInt32(reader.GetOrdinal("incident_id")),
                                    asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                    reported_by = reader.GetInt32(reader.GetOrdinal("reported_by")),
                                    incident_type = reader.GetString(reader.GetOrdinal("incident_type")),
                                    description = reader.GetString(reader.GetOrdinal("description")),
                                    location = JsonConvert.DeserializeObject<GeoJsonGeometry>(reader.GetString(reader.GetOrdinal("location"))),
                                    priority = reader.GetString(reader.GetOrdinal("priority")),
                                    status = reader.GetString(reader.GetOrdinal("status")),
                                    reported_at = reader.GetDateTime(reader.GetOrdinal("reported_at")),
                                    resolved_at = reader.GetDateTime(reader.GetOrdinal("resolved_at")),
                                    notes = reader.GetString(reader.GetOrdinal("notes")),
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

        public async Task<IncidentsResponse?> CreateIncident(IncidentsRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO incidents 
                (asset_id, reported_by, incident_type,description, location, priority,status,resolved_at,notes)
                VALUES (@asset_id, @reported_by, @incident_type,@description, ST_Transform(ST_GeomFromGeoJSON(@location), 3405), @priority, @status,@resolved_at,@notes)
                RETURNING incident_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@asset_id", entity.asset_id);
                        cmd.Parameters.AddWithValue("@reported_by", entity.reported_by);
                        cmd.Parameters.AddWithValue("@incident_type", entity.incident_type);
                        cmd.Parameters.AddWithValue("@description", entity.description);
                        cmd.Parameters.AddWithValue("@location", JsonConvert.SerializeObject(entity.location));
                        cmd.Parameters.AddWithValue("@priority", entity.priority);
                        cmd.Parameters.AddWithValue("@status", entity.status);
                        cmd.Parameters.AddWithValue("@resolved_at", entity.resolved_at);
                        cmd.Parameters.AddWithValue("@notes", entity.notes);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetIncidentById(newId);
                    }
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }
        public async Task<IncidentsResponse?> UpdateIncident(int id, IncidentsRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE incidents SET
                    asset_id  = @asset_id,
                    reported_by = @reported_by,
                    incident_type = @incident_type,
                    description = @description,
                    location = ST_Transform(ST_GeomFromGeoJSON(@location), 3405),
                    priority = @priority,
                    status = @status,
                    resolved_at = @resolved_at,
                    notes = @notes
                WHERE incident_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@asset_id", entity.asset_id);
                        cmd.Parameters.AddWithValue("@reported_by", entity.reported_by);
                        cmd.Parameters.AddWithValue("@incident_type", entity.incident_type);
                        cmd.Parameters.AddWithValue("@description", entity.description);
                        cmd.Parameters.AddWithValue("@location", JsonConvert.SerializeObject(entity.location));
                        cmd.Parameters.AddWithValue("@priority", entity.priority);
                        cmd.Parameters.AddWithValue("@status", entity.status);
                        cmd.Parameters.AddWithValue("@resolved_at", entity.resolved_at);
                        cmd.Parameters.AddWithValue("@notes", entity.notes);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetIncidentById(id);
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

        public async Task<bool> DeleteIncident(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM incidents WHERE incident_id = @id";

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
