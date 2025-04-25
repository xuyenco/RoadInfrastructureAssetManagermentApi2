using Microsoft.Extensions.Logging; 
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class IncidentImageService : IIncidentImageService
    {
        private readonly string _connectionString;
        private readonly ILogger<IncidentImageService> _logger; 

        public IncidentImageService(string connectionString, ILogger<IncidentImageService> logger) 
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<IEnumerable<IncidentImageResponse>> GetAllIncidentImages()
        {
            var incidentImages = new List<IncidentImageResponse>();

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM incident_images";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var incidentImage = new IncidentImageResponse
                            {
                                incident_image_id = reader.GetInt32(reader.GetOrdinal("incident_image_id")),
                                incident_id = reader.GetInt32(reader.GetOrdinal("incident_id")),
                                image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
                                image_name = reader.IsDBNull(reader.GetOrdinal("image_name")) ? null : reader.GetString(reader.GetOrdinal("image_name")),
                                image_public_id = reader.IsDBNull(reader.GetOrdinal("image_public_id")) ? null : reader.GetString(reader.GetOrdinal("image_public_id")),
                                created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            };
                            incidentImages.Add(incidentImage);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} incident images successfully", incidentImages.Count); 
                    return incidentImages;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve incident images from database"); 
                    throw new InvalidOperationException("Failed to retrieve Incident Images from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<IncidentImageResponse>> GetAllIncidentImagesByIncidentId(int incidentId)
        {
            var incidentImages = new List<IncidentImageResponse>();

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM incident_images WHERE incident_id = @incidentId";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@incidentId", incidentId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var incidentImage = new IncidentImageResponse
                                {
                                    incident_image_id = reader.GetInt32(reader.GetOrdinal("incident_image_id")),
                                    incident_id = reader.GetInt32(reader.GetOrdinal("incident_id")),
                                    image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
                                    image_name = reader.IsDBNull(reader.GetOrdinal("image_name")) ? null : reader.GetString(reader.GetOrdinal("image_name")),
                                    image_public_id = reader.IsDBNull(reader.GetOrdinal("image_public_id")) ? null : reader.GetString(reader.GetOrdinal("image_public_id")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                incidentImages.Add(incidentImage);
                            }
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} incident images for incident ID {IncidentId} successfully", incidentImages.Count, incidentId); 
                    return incidentImages;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve incident images for incident ID {IncidentId}", incidentId); 
                    throw new InvalidOperationException("Failed to retrieve Incident Images from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IncidentImageResponse?> GetIncidentImageById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM incident_images WHERE incident_image_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var incidentImage = new IncidentImageResponse
                                {
                                    incident_image_id = reader.GetInt32(reader.GetOrdinal("incident_image_id")),
                                    incident_id = reader.GetInt32(reader.GetOrdinal("incident_id")),
                                    image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
                                    image_name = reader.IsDBNull(reader.GetOrdinal("image_name")) ? null : reader.GetString(reader.GetOrdinal("image_name")),
                                    image_public_id = reader.IsDBNull(reader.GetOrdinal("image_public_id")) ? null : reader.GetString(reader.GetOrdinal("image_public_id")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                _logger.LogInformation("Retrieved incident image with ID {IncidentImageId} successfully", id); 
                                return incidentImage;
                            }
                            _logger.LogWarning("Incident image with ID {IncidentImageId} not found", id); 
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve incident image with ID {IncidentImageId}", id);
                    throw new InvalidOperationException($"Failed to retrieve incident image with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IncidentImageResponse?> CreateIncidentImage(IncidentImageRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO incident_images 
                (incident_id, image_url, image_public_id, image_name)
                VALUES (@incident_id, @image_url, @image_public_id, @image_name)
                RETURNING incident_image_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@incident_id", entity.incident_id);
                        cmd.Parameters.AddWithValue("@image_url", (object)entity.image_url ?? DBNull.Value); 
                        cmd.Parameters.AddWithValue("@image_public_id", (object)entity.image_public_id ?? DBNull.Value); 
                        cmd.Parameters.AddWithValue("@image_name", (object)entity.image_name ?? DBNull.Value); 

                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        _logger.LogInformation("Created incident image with ID {IncidentImageId} successfully", newId); 
                        return await GetIncidentImageById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to create incident image: Invalid incident ID {IncidentId}", entity.incident_id); 
                        throw new InvalidOperationException($"Incident ID {entity.incident_id} does not exist.", ex);
                    }
                    _logger.LogError(ex, "Failed to create incident image"); 
                    throw new InvalidOperationException($"Failed to create incident image: {ex.ToString()}");
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IncidentImageResponse?> UpdateIncidentImage(int id, IncidentImageRequest entity)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE incident_images SET
                    incident_id = @incident_id,
                    image_url = @image_url,
                    image_public_id = @image_public_id,
                    image_name = @image_name
                WHERE incident_image_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@incident_id", entity.incident_id);
                        cmd.Parameters.AddWithValue("@image_url", (object)entity.image_url ?? DBNull.Value); 
                        cmd.Parameters.AddWithValue("@image_public_id", (object)entity.image_public_id ?? DBNull.Value); 
                        cmd.Parameters.AddWithValue("@image_name", (object)entity.image_name ?? DBNull.Value); 

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Updated incident image with ID {IncidentImageId} successfully", id); 
                            return await GetIncidentImageById(id);
                        }
                        _logger.LogWarning("Incident image with ID {IncidentImageId} not found for update", id); 
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to update incident image with ID {IncidentImageId}: Invalid incident ID {IncidentId}", id, entity.incident_id); // Log lỗi khóa ngoại
                        throw new InvalidOperationException($"Incident ID {entity.incident_id} does not exist.", ex);
                    }
                    _logger.LogError(ex, "Failed to update incident image with ID {IncidentImageId}", id); 
                    throw new InvalidOperationException($"Failed to update incident image with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> DeleteIncidentImage(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM incident_images WHERE incident_image_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Deleted incident image with ID {IncidentImageId} successfully", id); 
                            return true;
                        }
                        _logger.LogWarning("Incident image with ID {IncidentImageId} not found for deletion", id); 
                        return false;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to delete incident image with ID {IncidentImageId}: Referenced by other records", id); 
                        throw new InvalidOperationException($"Cannot delete incident images with ID {id} because it is referenced by other records (e.g. incidents).", ex);
                    }
                    _logger.LogError(ex, "Failed to delete incident image with ID {IncidentImageId}", id); 
                    throw new InvalidOperationException($"Failed to delete incident images with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> DeleteIncidentImageByIncidentId(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM incident_images WHERE incident_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Deleted {Count} incident images for incident ID {IncidentId} successfully", affectedRows, id); 
                            return true;
                        }
                        _logger.LogWarning("No incident images found for incident ID {IncidentId} for deletion", id); 
                        return false;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to delete incident images for incident ID {IncidentId}: Referenced by other records", id); 
                        throw new InvalidOperationException($"Cannot delete incident images with incident Id {id} because it is referenced by other records (e.g. incidents).", ex);
                    }
                    _logger.LogError(ex, "Failed to delete incident images for incident ID {IncidentId}", id); 
                    throw new InvalidOperationException($"Failed to delete incident images with incident Id {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }
    }
}