using Newtonsoft.Json;
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Geometry;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class IncidentService : IIncidentsService
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
                var sql = "SELECT incident_id, address, ST_AsGeoJSON(geometry) as geometry, route, severity_level, damage_level, processing_status, task_id, created_at FROM incidents";

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
                                address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                                geometry = ParseGeoJson(reader.GetString(reader.GetOrdinal("geometry")), "geometry"),
                                route = reader.IsDBNull(reader.GetOrdinal("route")) ? null : reader.GetString(reader.GetOrdinal("route")),
                                severity_level = reader.IsDBNull(reader.GetOrdinal("severity_level")) ? null : reader.GetString(reader.GetOrdinal("severity_level")),
                                damage_level = reader.IsDBNull(reader.GetOrdinal("damage_level")) ? null : reader.GetString(reader.GetOrdinal("damage_level")),
                                processing_status = reader.IsDBNull(reader.GetOrdinal("processing_status")) ? null : reader.GetString(reader.GetOrdinal("processing_status")),
                                task_id = reader.IsDBNull (reader.GetOrdinal("task_id")) ? null : reader.GetInt32 (reader.GetOrdinal("task_id")),
                                created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
                            };
                            incidents.Add(incident);
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException($"Failed to retrieve incidents from database: {ex}", ex);
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
                var sql = "SELECT incident_id, address, ST_AsGeoJSON(geometry) as geometry, route, severity_level, damage_level, processing_status, task_id, created_at FROM incidents WHERE incident_id = @id";

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
                                    address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                                    geometry = ParseGeoJson(reader.GetString(reader.GetOrdinal("geometry")), "geometry"),
                                    route = reader.IsDBNull(reader.GetOrdinal("route")) ? null : reader.GetString(reader.GetOrdinal("route")),
                                    severity_level = reader.IsDBNull(reader.GetOrdinal("severity_level")) ? null : reader.GetString(reader.GetOrdinal("severity_level")),
                                    damage_level = reader.IsDBNull(reader.GetOrdinal("damage_level")) ? null : reader.GetString(reader.GetOrdinal("damage_level")),
                                    processing_status = reader.IsDBNull(reader.GetOrdinal("processing_status")) ? null : reader.GetString(reader.GetOrdinal("processing_status")),
                                    task_id = reader.IsDBNull(reader.GetOrdinal("task_id")) ? null : reader.GetInt32(reader.GetOrdinal("task_id")),
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                            }
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException($"Failed to retrieve incident with ID {id} because: {ex}.");
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
                (address, geometry, route, severity_level, damage_level, processing_status,task_id)
                VALUES (@address, ST_SetSRID(ST_GeomFromGeoJSON(@geometry), 3405), @route, @severity_level, @damage_level, @processing_status, @task_id)
                RETURNING incident_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@address", (object)entity.address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@geometry", JsonConvert.SerializeObject(entity.geometry));
                        cmd.Parameters.AddWithValue("@route", (object)entity.route ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@severity_level", entity.severity_level);
                        cmd.Parameters.AddWithValue("@damage_level", entity.damage_level);
                        cmd.Parameters.AddWithValue("@processing_status", entity.processing_status);
                        cmd.Parameters.AddWithValue("@task_id", (object)entity.task_id ?? DBNull.Value);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetIncidentById(newId)!;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
                    }
                    throw new InvalidOperationException($"Failed to create incident because {ex}",ex);
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
                    address = @address,
                    geometry = ST_SetSRID(ST_GeomFromGeoJSON(@geometry), 3405),
                    route = @route,
                    severity_level = @severity_level,
                    damage_level = @damage_level,
                    processing_status = @processing_status,
                    task_id = @task_id
                WHERE incident_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@address", (object)entity.address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@geometry", JsonConvert.SerializeObject(entity.geometry));
                        cmd.Parameters.AddWithValue("@route", (object)entity.route ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@severity_level", entity.severity_level);
                        cmd.Parameters.AddWithValue("@damage_level", entity.damage_level);
                        cmd.Parameters.AddWithValue("@processing_status", entity.processing_status);
                        cmd.Parameters.AddWithValue("@task_id", (object)entity.task_id ?? DBNull.Value);

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
                    if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
                    }
                    throw new InvalidOperationException($"Failed to update incident with ID {id} because {ex}.", ex);
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
                        throw new InvalidOperationException($"Cannot delete incident with ID {id} because it is referenced by other records (e.g., incident_images).", ex);
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
            if (string.IsNullOrWhiteSpace(entity.severity_level))
            {
                throw new ArgumentException("Severity level cannot be empty.");
            }
            if (string.IsNullOrWhiteSpace(entity.damage_level))
            {
                throw new ArgumentException("Damage level cannot be empty.");
            }
            if (string.IsNullOrWhiteSpace(entity.processing_status))
            {
                throw new ArgumentException("Processing status cannot be empty.");
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