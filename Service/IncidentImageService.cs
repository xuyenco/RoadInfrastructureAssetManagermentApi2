using Npgsql;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace Road_Infrastructure_Asset_Management.Service
{
    public class IncidentImageService : IIncidentImageService
    {
        private readonly string _connectionString;
        public IncidentImageService(string connectionString)
        {
            _connectionString = connectionString;
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
                               incident_images_id = reader.GetInt32(reader.GetOrdinal("incident_images_id")),
                               incident_id = reader.GetInt32(reader.GetOrdinal("incident_id")),
                               image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url"))
                            };
                            incidentImages.Add(incidentImage);
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to retrieve Incident Images from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
                return incidentImages;
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
                                    incident_images_id = reader.GetInt32(reader.GetOrdinal("incident_images_id")),
                                    incident_id = reader.GetInt32(reader.GetOrdinal("incident_id")),
                                    image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url"))
                                };
                                incidentImages.Add(incidentImage);
                            }
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to retrieve Incident Images from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
                return incidentImages;
            }
        }

        public async Task<IncidentImageResponse?> GetIncidentImageById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM incident_images WHERE incident_images_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new IncidentImageResponse
                                {
                                    incident_images_id = reader.GetInt32(reader.GetOrdinal("incident_images_id")),
                                    incident_id = reader.GetInt32(reader.GetOrdinal("incident_id")),
                                    image_url = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url"))
                                };
                            }
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
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
                (incident_id, image_url)
                VALUES (@incident_id, @image_url)
                RETURNING incident_images_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@incident_id", entity.incident_id);
                        cmd.Parameters.AddWithValue("@image_url", entity.image_url);
                        
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetIncidentImageById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to create user.", ex);
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
                    image_url = @image_url
                WHERE incident_images_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@incident_id", entity.incident_id);
                        cmd.Parameters.AddWithValue("@image_url", entity.image_url);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetIncidentImageById(id);
                        }
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {

                    throw new InvalidOperationException($"Failed to update user with ID {id}.", ex);
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
                var sql = "DELETE FROM incident_images WHERE incident_images_id = @id";

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
                        throw new InvalidOperationException($"Cannot delete incident images with ID {id} because it is referenced by other records (e.g. incidents).", ex);
                    }
                    throw new InvalidOperationException($"Failed to delete incident images with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

    }
}
