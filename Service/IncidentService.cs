using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Geometry;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;
using System.Text;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class IncidentService : IIncidentsService
    {
        private readonly string _connectionString;
        private readonly ILogger<IncidentService> _logger; 

        public IncidentService(string connectionString, ILogger<IncidentService> logger) 
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<IEnumerable<IncidentsResponse>> GetAllIncidents()
        {
            var incidents = new List<IncidentsResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT incident_id, address, incident_type, ST_AsGeoJSON(geometry) as geometry, route, severity_level, damage_level, processing_status, task_id, created_at FROM incidents";

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
                                incident_type = reader.IsDBNull(reader.GetOrdinal("incident_type")) ? null : reader.GetString(reader.GetOrdinal("incident_type")),
                                geometry = ParseGeoJson(reader.GetString(reader.GetOrdinal("geometry")), "geometry"),
                                route = reader.IsDBNull(reader.GetOrdinal("route")) ? null : reader.GetString(reader.GetOrdinal("route")),
                                severity_level = reader.IsDBNull(reader.GetOrdinal("severity_level")) ? null : reader.GetString(reader.GetOrdinal("severity_level")),
                                damage_level = reader.IsDBNull(reader.GetOrdinal("damage_level")) ? null : reader.GetString(reader.GetOrdinal("damage_level")),
                                processing_status = reader.IsDBNull(reader.GetOrdinal("processing_status")) ? null : reader.GetString(reader.GetOrdinal("processing_status")),
                                task_id = reader.IsDBNull(reader.GetOrdinal("task_id")) ? null : reader.GetInt32(reader.GetOrdinal("task_id")),
                                created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
                            };
                            incidents.Add(incident);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} incidents successfully", incidents.Count);
                    return incidents;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve incidents from database"); 
                    throw new InvalidOperationException($"Failed to retrieve incidents from database: {ex}", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IncidentsResponse?> GetIncidentById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT incident_id, address, incident_type, ST_AsGeoJSON(geometry) as geometry, route, severity_level, damage_level, processing_status, task_id, created_at FROM incidents WHERE incident_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var incident = new IncidentsResponse
                                {
                                    incident_id = reader.GetInt32(reader.GetOrdinal("incident_id")),
                                    address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                                    incident_type = reader.IsDBNull(reader.GetOrdinal("incident_type")) ? null : reader.GetString(reader.GetOrdinal("incident_type")),
                                    geometry = ParseGeoJson(reader.GetString(reader.GetOrdinal("geometry")), "geometry"),
                                    route = reader.IsDBNull(reader.GetOrdinal("route")) ? null : reader.GetString(reader.GetOrdinal("route")),
                                    severity_level = reader.IsDBNull(reader.GetOrdinal("severity_level")) ? null : reader.GetString(reader.GetOrdinal("severity_level")),
                                    damage_level = reader.IsDBNull(reader.GetOrdinal("damage_level")) ? null : reader.GetString(reader.GetOrdinal("damage_level")),
                                    processing_status = reader.IsDBNull(reader.GetOrdinal("processing_status")) ? null : reader.GetString(reader.GetOrdinal("processing_status")),
                                    task_id = reader.IsDBNull(reader.GetOrdinal("task_id")) ? null : reader.GetInt32(reader.GetOrdinal("task_id")),
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                _logger.LogInformation("Retrieved incident with ID {IncidentId} successfully", id);
                                return incident;
                            }
                            _logger.LogWarning("Incident with ID {IncidentId} not found", id); 
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve incident with ID {IncidentId}", id); 
                    throw new InvalidOperationException($"Failed to retrieve incident with ID {id}: {ex}.");
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<(IEnumerable<IncidentsResponse> Incidents, int TotalCount)> GetIncidentsPagination(int page, int pageSize, string searchTerm, int searchField)
        {
            var incidents = new List<IncidentsResponse>();
            int totalCount = 0;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Xây dựng câu lệnh truy vấn
                var sqlBuilder = new StringBuilder(@"SELECT incident_id, address, incident_type, ST_AsGeoJSON(geometry) as geometry, route, 
                                            severity_level, damage_level, processing_status, task_id, created_at 
                                            FROM incidents");
                var countSql = "SELECT COUNT(*) FROM incidents";
                var parameters = new List<NpgsqlParameter>();

                // Thêm điều kiện tìm kiếm nếu có
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = $"%{searchTerm.ToLower()}%"; // Chuẩn bị cho ILIKE
                    string condition = searchField switch
                    {
                        0 => "CAST(incident_id AS TEXT) ILIKE @searchTerm", // ID sự cố
                        1 => "LOWER(address) ILIKE @searchTerm",            // Địa chỉ
                        2 => "LOWER(route) ILIKE @searchTerm",              // Tuyến đường
                        3 => "LOWER(severity_level) ILIKE @searchTerm",     // Mức độ nghiêm trọng
                        4 => "LOWER(damage_level) ILIKE @searchTerm",       // Mức độ hư hỏng
                        5 => "LOWER(processing_status) ILIKE @searchTerm",  // Trạng thái xử lý
                        6 => "TO_CHAR(created_at, 'DD/MM/YYYY HH24:MI') ILIKE @searchTerm", // Ngày tạo
                        7 => "LOWER(incident_type) ILIKE @incident_type",
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
                sqlBuilder.Append(" ORDER BY incident_id");
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

                    // Lấy danh sách sự cố
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
                                var incident = new IncidentsResponse
                                {
                                    incident_id = reader.GetInt32(reader.GetOrdinal("incident_id")),
                                    address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                                    incident_type = reader.IsDBNull(reader.GetOrdinal("incident_type")) ? null : reader.GetString(reader.GetOrdinal("incident_type")),
                                    geometry = ParseGeoJson(reader.GetString(reader.GetOrdinal("geometry")), "geometry"),
                                    route = reader.IsDBNull(reader.GetOrdinal("route")) ? null : reader.GetString(reader.GetOrdinal("route")),
                                    severity_level = reader.IsDBNull(reader.GetOrdinal("severity_level")) ? null : reader.GetString(reader.GetOrdinal("severity_level")),
                                    damage_level = reader.IsDBNull(reader.GetOrdinal("damage_level")) ? null : reader.GetString(reader.GetOrdinal("damage_level")),
                                    processing_status = reader.IsDBNull(reader.GetOrdinal("processing_status")) ? null : reader.GetString(reader.GetOrdinal("processing_status")),
                                    task_id = reader.IsDBNull(reader.GetOrdinal("task_id")) ? null : reader.GetInt32(reader.GetOrdinal("task_id")),
                                    created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                incidents.Add(incident);
                            }
                        }
                    }

                    _logger.LogInformation("Retrieved {Count} incidents for page {Page} with total count {TotalCount}", incidents.Count, page, totalCount);
                    return (incidents, totalCount);
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve incidents with pagination and search");
                    throw new InvalidOperationException("Failed to retrieve incidents from database.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
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
                (address, incident_type, geometry, route, severity_level, damage_level, processing_status, task_id)
                VALUES (@address, @incident_type, ST_SetSRID(ST_GeomFromGeoJSON(@geometry), 3405), @route, @severity_level, @damage_level, @processing_status, @task_id)
                RETURNING incident_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@address", (object)entity.address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@incident_type", (object)entity.incident_type ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@geometry", JsonConvert.SerializeObject(entity.geometry));
                        cmd.Parameters.AddWithValue("@route", (object)entity.route ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@severity_level", entity.severity_level);
                        cmd.Parameters.AddWithValue("@damage_level", entity.damage_level);
                        cmd.Parameters.AddWithValue("@processing_status", entity.processing_status);
                        cmd.Parameters.AddWithValue("@task_id", (object)entity.task_id ?? DBNull.Value);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        _logger.LogInformation("Created incident with ID {IncidentId} successfully", newId); 
                        return await GetIncidentById(newId)!;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "22023") 
                    {
                        _logger.LogError(ex, "Failed to create incident: Invalid GeoJSON format for geometry");
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
                    }
                    _logger.LogError(ex, "Failed to create incident");
                    throw new InvalidOperationException($"Failed to create incident because {ex}", ex);
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
                    incident_type = @incident_type,
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
                        cmd.Parameters.AddWithValue("@incident_type", (object)entity.incident_type ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@geometry", JsonConvert.SerializeObject(entity.geometry));
                        cmd.Parameters.AddWithValue("@route", (object)entity.route ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@severity_level", entity.severity_level);
                        cmd.Parameters.AddWithValue("@damage_level", entity.damage_level);
                        cmd.Parameters.AddWithValue("@processing_status", entity.processing_status);
                        cmd.Parameters.AddWithValue("@task_id", (object)entity.task_id ?? DBNull.Value);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Updated incident with ID {IncidentId} successfully", id); 
                            return await GetIncidentById(id);
                        }
                        _logger.LogWarning("Incident with ID {IncidentId} not found for update", id);
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "22023") // Invalid GeoJSON
                    {
                        _logger.LogError(ex, "Failed to update incident with ID {IncidentId}: Invalid GeoJSON format for geometry", id); 
                        throw new InvalidOperationException("Invalid GeoJSON format for geometry.", ex);
                    }
                    _logger.LogError(ex, "Failed to update incident with ID {IncidentId}", id); 
                    throw new InvalidOperationException($"Failed to update incident with ID {id}: {ex}.", ex);
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
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Deleted incident with ID {IncidentId} successfully", id); 
                            return true;
                        }
                        _logger.LogWarning("Incident with ID {IncidentId} not found for deletion", id);
                        return false;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to delete incident with ID {IncidentId}: Referenced by other records", id); 
                        throw new InvalidOperationException($"Cannot delete incident with ID {id} because it is referenced by other records (e.g., incident_images).", ex);
                    }
                    _logger.LogError(ex, "Failed to delete incident with ID {IncidentId}", id);
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
            if (string.IsNullOrWhiteSpace(entity.severity_level))
            {
                _logger.LogWarning("Validation failed: Severity level cannot be empty");
                throw new ArgumentException("Severity level cannot be empty.");
            }
            if (string.IsNullOrWhiteSpace(entity.damage_level))
            {
                _logger.LogWarning("Validation failed: Damage level cannot be empty"); 
                throw new ArgumentException("Damage level cannot be empty.");
            }
            if (string.IsNullOrWhiteSpace(entity.processing_status))
            {
                _logger.LogWarning("Validation failed: Processing status cannot be empty");
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
                _logger.LogError(ex, "Failed to parse GeoJSON for {FieldName}", fieldName); 
                throw new InvalidOperationException($"Invalid GeoJSON format for {fieldName}.", ex);
            }
        }
    }
}