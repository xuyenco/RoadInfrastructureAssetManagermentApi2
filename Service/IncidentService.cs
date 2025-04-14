using Newtonsoft.Json;
using Npgsql;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Geometry;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Road_Infrastructure_Asset_Management.Service
{
    public class IncidentService : IIncidentsService
    {
        private readonly string _connectionString;
        private static readonly string[] ValidPriorities = { "low", "medium", "high" };
        private static readonly string[] ValidStatuses = { "reported", "in_progress", "resolved" };

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
                var sql = "SELECT incident_id, asset_id, reported_by, incident_type, description, ST_AsGeoJSON(location) as location, priority, status, reported_at, resolved_at, notes FROM incidents";

                try
                {
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
                                location = ParseGeoJson(reader.GetString(reader.GetOrdinal("location")), "location"),
                                priority = reader.GetString(reader.GetOrdinal("priority")),
                                status = reader.GetString(reader.GetOrdinal("status")),
                                reported_at = reader.GetDateTime(reader.GetOrdinal("reported_at")),
                                resolved_at = reader.IsDBNull(reader.GetOrdinal("resolved_at")) ? null : reader.GetDateTime(reader.GetOrdinal("resolved_at")),
                                notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes"))
                            };
                            incidents.Add(incident);
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to retrieve incidents from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
                return incidents;
            }
        }

        public async Task<IncidentsResponse?> GetIncidentById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT incident_id, asset_id, reported_by, incident_type, description, ST_AsGeoJSON(location) as location, priority, status, reported_at, resolved_at, notes FROM incidents WHERE incident_id = @id";

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
                                    location = ParseGeoJson(reader.GetString(reader.GetOrdinal("location")), "location"),
                                    priority = reader.GetString(reader.GetOrdinal("priority")),
                                    status = reader.GetString(reader.GetOrdinal("status")),
                                    reported_at = reader.GetDateTime(reader.GetOrdinal("reported_at")),
                                    resolved_at = reader.IsDBNull(reader.GetOrdinal("resolved_at")) ? null : reader.GetDateTime(reader.GetOrdinal("resolved_at")),
                                    notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes"))
                                };
                            }
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException($"Failed to retrieve incident with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IncidentsResponse> CreateIncident(IncidentsRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO incidents 
                (asset_id, reported_by, incident_type, description, location, priority, status, resolved_at, notes)
                VALUES (@asset_id, @reported_by, @incident_type, @description, ST_SetSRID(ST_GeomFromGeoJSON(@location), 3405), @priority, @status, @resolved_at, @notes)
                RETURNING incident_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@asset_id", entity.asset_id != 0 ? entity.asset_id : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@reported_by", entity.reported_by);
                        cmd.Parameters.AddWithValue("@incident_type", entity.incident_type);
                        cmd.Parameters.AddWithValue("@description", entity.description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@location", JsonConvert.SerializeObject(entity.location));
                        cmd.Parameters.AddWithValue("@priority", entity.priority);
                        cmd.Parameters.AddWithValue("@status", entity.status);
                        cmd.Parameters.AddWithValue("@resolved_at", entity.resolved_at ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@notes", entity.notes ?? (object)DBNull.Value);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetIncidentById(newId)!;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        if (ex.Message.Contains("asset_id"))
                            throw new InvalidOperationException($"Asset ID {entity.asset_id} does not exist.", ex);
                        if (ex.Message.Contains("reported_by"))
                            throw new InvalidOperationException($"User ID {entity.reported_by} does not exist.", ex);
                    }
                    else if (ex.SqlState == "23514") // Check constraint violation
                    {
                        if (ex.Message.Contains("priority"))
                            throw new InvalidOperationException($"Priority must be one of: {string.Join(", ", ValidPriorities)}.", ex);
                        if (ex.Message.Contains("status"))
                            throw new InvalidOperationException($"Status must be one of: {string.Join(", ", ValidStatuses)}.", ex);
                    }
                    else if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        throw new InvalidOperationException("Invalid GeoJSON format for location.", ex);
                    }
                    throw new InvalidOperationException("Failed to create incident.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IncidentsResponse?> UpdateIncident(int id, IncidentsRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE incidents SET
                    asset_id = @asset_id,
                    reported_by = @reported_by,
                    incident_type = @incident_type,
                    description = @description,
                    location = ST_SetSRID(ST_GeomFromGeoJSON(@location), 3405),
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
                        cmd.Parameters.AddWithValue("@asset_id", entity.asset_id != 0 ? entity.asset_id : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@reported_by", entity.reported_by);
                        cmd.Parameters.AddWithValue("@incident_type", entity.incident_type);
                        cmd.Parameters.AddWithValue("@description", entity.description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@location", JsonConvert.SerializeObject(entity.location));
                        cmd.Parameters.AddWithValue("@priority", entity.priority);
                        cmd.Parameters.AddWithValue("@status", entity.status);
                        cmd.Parameters.AddWithValue("@resolved_at", entity.resolved_at ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@notes", entity.notes ?? (object)DBNull.Value);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetIncidentById(id);
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
                        if (ex.Message.Contains("reported_by"))
                            throw new InvalidOperationException($"User ID {entity.reported_by} does not exist.", ex);
                    }
                    else if (ex.SqlState == "23514") // Check constraint violation
                    {
                        if (ex.Message.Contains("priority"))
                            throw new InvalidOperationException($"Priority must be one of: {string.Join(", ", ValidPriorities)}.", ex);
                        if (ex.Message.Contains("status"))
                            throw new InvalidOperationException($"Status must be one of: {string.Join(", ", ValidStatuses)}.", ex);
                    }
                    else if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        throw new InvalidOperationException("Invalid GeoJSON format for location.", ex);
                    }
                    throw new InvalidOperationException($"Failed to update incident with ID {id}.", ex);
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
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Cannot delete incident with ID {id} because it is referenced by other records (e.g., incident_history).", ex);
                    }
                    throw new InvalidOperationException($"Failed to delete incident with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        // Helper methods
        private void ValidateRequest(IncidentsRequest entity)
        {
            if (entity.reported_by <= 0)
            {
                throw new ArgumentException("Reported by user ID must be a positive integer.");
            }
            if (entity.asset_id < 0)
            {
                throw new ArgumentException("Asset ID cannot be negative.");
            }
            if (string.IsNullOrWhiteSpace(entity.incident_type))
            {
                throw new ArgumentException("Incident type cannot be empty.");
            }
            if (entity.location == null || string.IsNullOrEmpty(entity.location.type))
            {
                throw new ArgumentException("Location cannot be null or invalid.");
            }
            try
            {
                JsonConvert.SerializeObject(entity.location);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid GeoJSON format for location.", ex);
            }
            if (!ValidPriorities.Contains(entity.priority))
            {
                throw new ArgumentException($"Priority must be one of: {string.Join(", ", ValidPriorities)}.");
            }
            if (!ValidStatuses.Contains(entity.status))
            {
                throw new ArgumentException($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
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

        private async Task<bool> UserExists(int userId)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT COUNT(*) FROM users WHERE user_id = @userId";
                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        var count = (long)await cmd.ExecuteScalarAsync();
                        return count > 0;
                    }
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        private async Task<bool> AssetExists(int assetId)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT COUNT(*) FROM assets WHERE asset_id = @assetId";
                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@assetId", assetId);
                        var count = (long)await cmd.ExecuteScalarAsync();
                        return count > 0;
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