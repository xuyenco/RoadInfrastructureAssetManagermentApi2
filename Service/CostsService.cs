using Npgsql;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using System.Data.Common;

namespace Road_Infrastructure_Asset_Management.Service
{
    public class CostsService : ICostsService
    {
        private readonly string _connectionString;

        public CostsService(string connection)
        {
            _connectionString = connection;
        }

        public async Task<IEnumerable<CostsResponse>> GetAllCosts()
        {
            var costs = new List<CostsResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM costs";

                using (var cmd = new NpgsqlCommand(sql, _connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var cost = new CostsResponse
                        {
                            cost_id = reader.GetInt32(reader.GetOrdinal("cost_id")),
                            task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                            cost_type = reader.GetString(reader.GetOrdinal("cost_type")),
                            amount = reader.GetDouble(reader.GetOrdinal("amount")),
                            description = reader.GetString(reader.GetOrdinal("Description")),
                            date_incurred = reader.GetDateTime(reader.GetOrdinal("date_incurred")),
                            created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                        };
                        costs.Add(cost);
                    }
                }
            }
            return costs;
        }

        public async Task<CostsResponse?> GetCostById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString)) 
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM costs WHERE cost_id = @id";
                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new CostsResponse
                                {
                                    cost_id = reader.GetInt32(reader.GetOrdinal("cost_id")),
                                    task_id = reader.GetInt32(reader.GetOrdinal("task_id")),
                                    cost_type = reader.GetString(reader.GetOrdinal("cost_type")),
                                    amount = reader.GetDouble(reader.GetOrdinal("amount")),
                                    description = reader.GetString(reader.GetOrdinal("Description")),
                                    date_incurred = reader.GetDateTime(reader.GetOrdinal("date_incurred")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
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

        public async Task<CostsResponse?> CreateCost(CostsRequest entity)
        {
            using(var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO costs 
                (task_id, cost_type, amount, description,date_incurred)
                VALUES (@task_id, @cost_type, @amount, @description,@date_incurred)
                RETURNING cost_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@task_id", entity.task_id);
                        cmd.Parameters.AddWithValue("@cost_type", entity.cost_type);
                        cmd.Parameters.AddWithValue("@amount", entity.amount);
                        cmd.Parameters.AddWithValue("@description", entity.description);
                        cmd.Parameters.AddWithValue("@date_incurred", entity.date_incurred);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetCostById(newId);
                    }
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<CostsResponse?> UpdateCost(int id, CostsRequest entity)
        {
            using(var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE costs SET
                    task_id = @task_id,
                    cost_type = @cost_type,
                    amount = @amount,
                    description = @description:,
                    date_incurred = @date_incurred
                WHERE cost_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@task_id", entity.task_id);
                        cmd.Parameters.AddWithValue("@cost_type", entity.cost_type);
                        cmd.Parameters.AddWithValue("@amount", entity.amount);
                        cmd.Parameters.AddWithValue("@description", entity.description);
                        cmd.Parameters.AddWithValue("@date_incurred", entity.date_incurred);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetCostById(id);
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

        public async Task<bool> DeleteCost(int id)
        {
            using(var  _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM costs WHERE cost_id = @id";

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
