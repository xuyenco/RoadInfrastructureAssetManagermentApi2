using Microsoft.Extensions.Logging;
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class MaintenanceDocumentService : IMaintenanceDocumentService
    {
        private readonly string _connectionString;
        private readonly ILogger<MaintenanceDocumentService> _logger; 

        public MaintenanceDocumentService(string connection, ILogger<MaintenanceDocumentService> logger) 
        {
            _connectionString = connection;
            _logger = logger;
        }

        public async Task<IEnumerable<MaintenanceDocumentResponse>> GetAllMaintenanceDocuments()
        {
            var maintenanceDocuments = new List<MaintenanceDocumentResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM maintenance_documents";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var maintenanceDocument = new MaintenanceDocumentResponse
                            {
                                document_id = reader.GetInt32(reader.GetOrdinal("document_id")),
                                maintenance_id = reader.GetInt32(reader.GetOrdinal("maintenance_id")),
                                file_url = reader.GetString(reader.GetOrdinal("file_url")),
                                file_public_id = reader.GetString(reader.GetOrdinal("file_public_id")),
                                file_name = reader.GetString(reader.GetOrdinal("file_name")),
                                created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            };
                            maintenanceDocuments.Add(maintenanceDocument);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} maintenance documents successfully", maintenanceDocuments.Count);
                    return maintenanceDocuments;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve maintenance documents from database");
                    throw new InvalidOperationException("Failed to retrieve maintenance document from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<MaintenanceDocumentResponse>> GetMaintenanceDocumentByMaintenanceId(int id)
        {
            var maintenanceDocuments = new List<MaintenanceDocumentResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM maintenance_documents WHERE maintenance_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var maintenanceDocument = new MaintenanceDocumentResponse
                                {
                                    document_id = reader.GetInt32(reader.GetOrdinal("document_id")),
                                    maintenance_id = reader.GetInt32(reader.GetOrdinal("maintenance_id")),
                                    file_url = reader.GetString(reader.GetOrdinal("file_url")),
                                    file_public_id = reader.GetString(reader.GetOrdinal("file_public_id")),
                                    file_name = reader.GetString(reader.GetOrdinal("file_name")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                maintenanceDocuments.Add(maintenanceDocument);
                            }
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} maintenance documents for maintenance ID {MaintenanceId} successfully", maintenanceDocuments.Count, id);
                    return maintenanceDocuments;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve maintenance documents for maintenance ID {MaintenanceId}", id);
                    throw new InvalidOperationException($"Failed to retrieve maintenance document with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<MaintenanceDocumentResponse?> GetMaintenanceDocumentById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM maintenance_documents WHERE document_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var maintenanceDocument = new MaintenanceDocumentResponse
                                {
                                    document_id = reader.GetInt32(reader.GetOrdinal("document_id")),
                                    maintenance_id = reader.GetInt32(reader.GetOrdinal("maintenance_id")),
                                    file_url = reader.GetString(reader.GetOrdinal("file_url")),
                                    file_public_id = reader.GetString(reader.GetOrdinal("file_public_id")),
                                    file_name = reader.GetString(reader.GetOrdinal("file_name")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                _logger.LogInformation("Retrieved maintenance document with ID {DocumentId} successfully", id);
                                return maintenanceDocument;
                            }
                            _logger.LogWarning("Maintenance document with ID {DocumentId} not found", id); 
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve maintenance document with ID {DocumentId}", id); 
                    throw new InvalidOperationException($"Failed to retrieve maintenance document with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<MaintenanceDocumentResponse?> CreateMaintenanceDocument(MaintenanceDocumentRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                    INSERT INTO maintenance_documents 
                    (maintenance_id, file_url, file_public_id, file_name)
                    VALUES (@maintenance_id, @file_url, @file_public_id, @file_name)
                    RETURNING document_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@maintenance_id", entity.maintenance_id);
                        cmd.Parameters.AddWithValue("@file_url", entity.file_url);
                        cmd.Parameters.AddWithValue("@file_public_id", entity.file_public_id);
                        cmd.Parameters.AddWithValue("@file_name", entity.file_name);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        _logger.LogInformation("Created maintenance document with ID {DocumentId} successfully", newId); 
                        return await GetMaintenanceDocumentById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to create maintenance document: Invalid maintenance ID {MaintenanceId}", entity.maintenance_id); // Log lỗi khóa ngoại
                        throw new InvalidOperationException($"Task ID {entity.maintenance_id} does not exist.", ex);
                    }
                    _logger.LogError(ex, "Failed to create maintenance document"); 
                    throw new InvalidOperationException("Failed to create maintenanceDocument.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<MaintenanceDocumentResponse?> UpdateMaintenanceDocument(int id, MaintenanceDocumentRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                    UPDATE maintenance_documents SET
                        maintenance_id = @maintenance_id,
                        file_url = @file_url,
                        file_public_id = @file_public_id,
                        file_name = @file_name
                    WHERE document_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@maintenance_id", entity.maintenance_id);
                        cmd.Parameters.AddWithValue("@file_url", entity.file_url);
                        cmd.Parameters.AddWithValue("@file_public_id", entity.file_public_id);
                        cmd.Parameters.AddWithValue("@file_name", entity.file_name);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Updated maintenance document with ID {DocumentId} successfully", id); 
                            return await GetMaintenanceDocumentById(id);
                        }
                        _logger.LogWarning("Maintenance document with ID {DocumentId} not found for update", id); 
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to update maintenance document with ID {DocumentId}: Invalid maintenance ID {MaintenanceId}", id, entity.maintenance_id); 
                        throw new InvalidOperationException($"Task ID {entity.maintenance_id} does not exist.", ex);
                    }
                    _logger.LogError(ex, "Failed to update maintenance document with ID {DocumentId}"); 
                    throw new InvalidOperationException($"Failed to update maintenance document with ID {id}", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> DeleteMaintenanceDocument(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM maintenance_documents WHERE document_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Deleted maintenance document with ID {DocumentId} successfully", id); 
                            return true;
                        }
                        _logger.LogWarning("Maintenance document with ID {DocumentId} not found for deletion", id);
                        return false;
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to delete maintenance document with ID {DocumentId}", id); 
                    throw new InvalidOperationException($"Failed to delete maintenance document with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> DeleteMaintenanceDocumentByMaintenanceId(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM maintenance_documents WHERE maintenance_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Deleted {Count} maintenance documents for maintenance ID {MaintenanceId} successfully", affectedRows, id); 
                            return true;
                        }
                        _logger.LogWarning("No maintenance documents found for maintenance ID {MaintenanceId} for deletion", id); 
                        return false;
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to delete maintenance documents for maintenance ID {MaintenanceId}", id); 
                    throw new InvalidOperationException($"Failed to delete maintenance document with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        // Helper method
        private void ValidateRequest(MaintenanceDocumentRequest entity)
        {
            if (entity.maintenance_id <= 0)
            {
                _logger.LogWarning("Validation failed: Maintenance ID must be a positive integer"); 
                throw new ArgumentException("Maintenance ID must be a positive integer.");
            }
        }
    }
}