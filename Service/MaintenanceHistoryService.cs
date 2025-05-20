using Microsoft.Extensions.Logging; 
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Response;
using Road_Infrastructure_Asset_Management_2.Model.Request;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class MaintenanceHistoryService : IMaintenanceHistoryService
    {
        private readonly string _connectionString;
        private readonly ILogger<MaintenanceHistoryService> _logger; 

        public MaintenanceHistoryService(string connectionString, ILogger<MaintenanceHistoryService> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<IEnumerable<MaintenanceHistoryResponse>> GetAllMaintenanceHistories()
        {
            var maintenanceHistories = new List<MaintenanceHistoryResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM maintenance_history";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var maintenanceHistory = new MaintenanceHistoryResponse
                            {
                                maintenance_id = reader.GetInt32(reader.GetOrdinal("maintenance_id")),
                                task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            };
                            maintenanceHistories.Add(maintenanceHistory);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} maintenance histories successfully", maintenanceHistories.Count); 
                    return maintenanceHistories;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve maintenance histories from database"); 
                    throw new InvalidOperationException("Failed to retrieve maintenance history from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<MaintenanceHistoryResponse>> GetMaintenanceHistoryByAssetId(int id)
        {
            var maintenanceHistories = new List<MaintenanceHistoryResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM maintenance_history WHERE asset_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var maintenanceHistory = new MaintenanceHistoryResponse
                                {
                                    maintenance_id = reader.GetInt32(reader.GetOrdinal("maintenance_id")),
                                    task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                    asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                maintenanceHistories.Add(maintenanceHistory);
                            }
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} maintenance histories for asset ID {AssetId} successfully", maintenanceHistories.Count, id); 
                    return maintenanceHistories;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve maintenance histories for asset ID {AssetId}", id); 
                    throw new InvalidOperationException($"Failed to retrieve maintenance history with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }


        public async Task<(IEnumerable<MaintenanceHistoryResponse>, int)> GetPagedMaintenanceHistoryByAssetId(int id, int currentPage = 1, int pageSize = 10, string searchTerm = "", int searchField = 0)
        {
            var maintenanceHistories = new List<MaintenanceHistoryResponse>();
            var totalCount = 0;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                try
                {
                    // Build SQL query for data with pagination and search
                    var whereClauses = new List<string> { "asset_id = @id" };
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        switch (searchField)
                        {
                            case 0: // maintenance_id
                                whereClauses.Add("maintenance_id::text ILIKE @searchTerm");
                                break;
                            case 1: // task_id
                                whereClauses.Add("task_id::text ILIKE @searchTerm");
                                break;
                            case 2: // created_at
                                whereClauses.Add("TO_CHAR(created_at, 'YYYY-MM-DD') ILIKE @searchTerm");
                                break;
                            default:
                                throw new ArgumentException("Invalid search field", nameof(searchField));
                        }
                    }

                    var whereClause = string.Join(" AND ", whereClauses);
                    var sqlData = $@"
                        SELECT * FROM maintenance_history 
                        WHERE {whereClause} 
                        ORDER BY created_at DESC 
                        LIMIT @pageSize OFFSET @offset";

                    var sqlCount = $@"SELECT COUNT(*) FROM maintenance_history WHERE {whereClause}";

                    // Calculate offset for pagination
                    var offset = (currentPage - 1) * pageSize;

                    // Execute count query
                    using (var countCmd = new NpgsqlCommand(sqlCount, connection))
                    {
                        countCmd.Parameters.AddWithValue("@id", id);
                        if (!string.IsNullOrEmpty(searchTerm))
                        {
                            countCmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
                        }
                        totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                    }

                    // Execute data query
                    using (var dataCmd = new NpgsqlCommand(sqlData, connection))
                    {
                        dataCmd.Parameters.AddWithValue("@id", id);
                        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);
                        dataCmd.Parameters.AddWithValue("@offset", offset);
                        if (!string.IsNullOrEmpty(searchTerm))
                        {
                            dataCmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
                        }

                        using (var reader = await dataCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var maintenanceHistory = new MaintenanceHistoryResponse
                                {
                                    maintenance_id = reader.GetInt32(reader.GetOrdinal("maintenance_id")),
                                    task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                    asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                maintenanceHistories.Add(maintenanceHistory);
                            }
                        }
                    }

                    _logger.LogInformation(
                        "Retrieved {Count} maintenance histories for asset ID {AssetId} (Page: {CurrentPage}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, SearchField: {SearchField}, TotalCount: {TotalCount})",
                        maintenanceHistories.Count, id, currentPage, pageSize, searchTerm, searchField, totalCount);

                    return (maintenanceHistories, totalCount);
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve paged maintenance histories for asset ID {AssetId} with parameters: Page={CurrentPage}, PageSize={PageSize}, SearchTerm={SearchTerm}, SearchField={SearchField}",
                        id, currentPage, pageSize, searchTerm, searchField);
                    throw new InvalidOperationException($"Failed to retrieve maintenance history for asset ID {id}.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }
        public async Task<MaintenanceHistoryResponse?> GetMaintenanceHistoryById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM maintenance_history WHERE maintenance_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var maintenanceHistory = new MaintenanceHistoryResponse
                                {
                                    maintenance_id = reader.GetInt32(reader.GetOrdinal("maintenance_id")),
                                    task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                    asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                _logger.LogInformation("Retrieved maintenance history with ID {MaintenanceId} successfully", id); 
                                return maintenanceHistory;
                            }
                            _logger.LogWarning("Maintenance history with ID {MaintenanceId} not found", id); 
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve maintenance history with ID {MaintenanceId}", id); 
                    throw new InvalidOperationException($"Failed to retrieve maintenance history with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<MaintenanceHistoryResponse?> CreateMaintenanceHistory(MaintenanceHistoryRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO maintenance_history 
                (asset_id, task_id)
                VALUES (@asset_id, @task_id)
                RETURNING maintenance_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@asset_id", entity.asset_id);
                        cmd.Parameters.AddWithValue("@task_id", entity.task_id);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        _logger.LogInformation("Created maintenance history with ID {MaintenanceId} successfully", newId); 
                        return await GetMaintenanceHistoryById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to create maintenance history: Invalid asset ID {AssetId} or task ID {TaskId}", entity.asset_id, entity.task_id); 
                        throw new InvalidOperationException($"Asset ID {entity.asset_id} or Task id {entity.task_id} does not exist.", ex);
                    }
                    _logger.LogError(ex, "Failed to create maintenance history"); 
                    throw new InvalidOperationException("Failed to create maintenance history.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<MaintenanceHistoryResponse?> UpdateMaintenanceHistory(int id, MaintenanceHistoryRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE maintenance_history SET
                    task_id = @task_id,
                    asset_id = @asset_id
                WHERE maintenance_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@asset_id", entity.asset_id);
                        cmd.Parameters.AddWithValue("@task_id", entity.task_id);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Updated maintenance history with ID {MaintenanceId} successfully", id); 
                            return await GetMaintenanceHistoryById(id);
                        }
                        _logger.LogWarning("Maintenance history with ID {MaintenanceId} not found for update", id); 
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to update maintenance history with ID {MaintenanceId}: Invalid asset ID {AssetId} or task ID {TaskId}", id, entity.asset_id, entity.task_id); // Log lỗi khóa ngoại
                        throw new InvalidOperationException($"Asset ID {entity.asset_id} or Task id {entity.task_id} does not exist.", ex);
                    }
                    _logger.LogError(ex, "Failed to update maintenance history with ID {MaintenanceId}");
                    throw new InvalidOperationException($"Failed to update maintenance history :{ex}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> DeleteMaintenanceHistory(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM maintenance_history WHERE maintenance_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Deleted maintenance history with ID {MaintenanceId} successfully", id);
                            return true;
                        }
                        _logger.LogWarning("Maintenance history with ID {MaintenanceId} not found for deletion", id);
                        return false;
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to delete maintenance history with ID {MaintenanceId}", id);
                    throw new InvalidOperationException($"Failed to delete maintenance history with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        // Helper method
        private void ValidateRequest(MaintenanceHistoryRequest entity)
        {
            if (entity.task_id <= 0)
            {
                _logger.LogWarning("Validation failed: Task ID must be a positive integer"); 
                throw new ArgumentException("Task Id must be a positive integer.");
            }
            if (entity.asset_id < 0)
            {
                _logger.LogWarning("Validation failed: Asset ID must be a positive integer"); 
                throw new ArgumentException("Asset Id must be a positive integer");
            }
        }
    }
}