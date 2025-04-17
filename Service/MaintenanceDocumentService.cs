using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class MaintenanceDocumentService : IMaintenanceDocumentService
    {
        private readonly string _connectionString;

        public MaintenanceDocumentService(string connection)
        {
            _connectionString = connection;
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
                                created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            };
                            maintenanceDocuments.Add(maintenanceDocument);
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to retrieve maintenance document from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync(); 
                }
                return maintenanceDocuments;
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
                                return new MaintenanceDocumentResponse
                                {
                                    document_id = reader.GetInt32(reader.GetOrdinal("document_id")),
                                    maintenance_id = reader.GetInt32(reader.GetOrdinal("maintenance_id")),
                                    file_url = reader.GetString(reader.GetOrdinal("file_url")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                            }
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
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
                    (maintenance_id, file_url)
                    VALUES (@maintenance_id, @file_url)
                    RETURNING document_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@maintenance_id", entity.maintenance_id);
                        cmd.Parameters.AddWithValue("@file_url", entity.file_url);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetMaintenanceDocumentById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Task ID {entity.maintenance_id} does not exist.", ex);
                    }
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
                        file_url = @file_url
                    WHERE document_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@maintenance_id", entity.maintenance_id);
                        cmd.Parameters.AddWithValue("@file_url", entity.file_url);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetMaintenanceDocumentById(id);
                        }
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Task ID {entity.maintenance_id} does not exist.", ex);
                    }
                    throw new InvalidOperationException("Failed to create maintenanceDocument.", ex);
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
                        return affectedRows > 0;
                    }
                }
                catch (NpgsqlException ex)
                {
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
                throw new ArgumentException("Maintenance ID must be a positive integer.");
            }
        }

    }
}
