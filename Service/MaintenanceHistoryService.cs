using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Response;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class MaintenanceHistoryService : IMaintenanceHistoryService
    {
        private readonly string _connectionString;

        public MaintenanceHistoryService(string connectionString)
        {
            _connectionString = connectionString;
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
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to retrieve maintenance history from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
                return maintenanceHistories;
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
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException($"Failed to retrieve maintenance history with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
                return maintenanceHistories;
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
                                return new MaintenanceHistoryResponse
                                {
                                    maintenance_id = reader.GetInt32(reader.GetOrdinal("maintenance_id")),
                                    task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                    asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                            }
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
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
                        return await GetMaintenanceHistoryById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Asset ID {entity.asset_id} or Task id {entity.task_id} does not exist.", ex);
                    }
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
                            return await GetMaintenanceHistoryById(id);
                        }
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Asset ID {entity.asset_id} or Task id {entity.task_id} does not exist.", ex);
                    }
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
                        return affectedRows > 0;
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException($"Failed to delete maintenance history with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }



        // Helper method
        private void ValidateRequest( MaintenanceHistoryRequest entity)
        {
            if (entity.task_id <= 0)
            {
                throw new ArgumentException("Task Id must be a positive integer.");
            }
            if (entity.asset_id < 0)
            {
                throw new ArgumentException("Asset Id must be a positive integer");
            }
        }
    }
}
