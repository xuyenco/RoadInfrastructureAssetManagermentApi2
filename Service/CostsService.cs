using Npgsql;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

                try
                {
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
                                description = reader.GetString(reader.GetOrdinal("description")), 
                                date_incurred = reader.GetDateTime(reader.GetOrdinal("date_incurred")),
                                created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            };
                            costs.Add(cost);
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to retrieve costs from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync(); 
                }
                return costs;
            }
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
                                    description = reader.GetString(reader.GetOrdinal("description")), 
                                    date_incurred = reader.GetDateTime(reader.GetOrdinal("date_incurred")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                            }
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException($"Failed to retrieve cost with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<CostsResponse?> CreateCost(CostsRequest entity)
        {
            ValidateRequest(entity);

            // Trích xuất fiscal_year từ date_incurred
            if (!entity.date_incurred.HasValue)
            {
                throw new ArgumentException("Date incurred cannot be null.");
            }
            int fiscalYear = entity.date_incurred.Value.Year;

            // Kiểm tra ngân sách
            await CheckBudgetLimit(fiscalYear, entity.amount);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                    INSERT INTO costs 
                    (task_id, cost_type, amount, description, date_incurred)
                    VALUES (@task_id, @cost_type, @amount, @description, @date_incurred)
                    RETURNING cost_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@task_id", entity.task_id);
                        cmd.Parameters.AddWithValue("@cost_type", entity.cost_type);
                        cmd.Parameters.AddWithValue("@amount", entity.amount);
                        cmd.Parameters.AddWithValue("@description", entity.description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@date_incurred", entity.date_incurred);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        return await GetCostById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Task ID {entity.task_id} does not exist.", ex);
                    }
                    throw new InvalidOperationException("Failed to create cost.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<CostsResponse?> UpdateCost(int id, CostsRequest entity)
        {
            ValidateRequest(entity);

            // Trích xuất fiscal_year từ date_incurred
            if (!entity.date_incurred.HasValue)
            {
                throw new ArgumentException("Date incurred cannot be null.");
            }
            int fiscalYear = entity.date_incurred.Value.Year;

            // Kiểm tra ngân sách, loại trừ amount cũ của cost_id
            await CheckBudgetLimit(fiscalYear, entity.amount, id);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                    UPDATE costs SET
                        task_id = @task_id,
                        cost_type = @cost_type,
                        amount = @amount,
                        description = @description,
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
                        cmd.Parameters.AddWithValue("@description", entity.description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@date_incurred", entity.date_incurred);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return await GetCostById(id);
                        }
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Task ID {entity.task_id} does not exist.", ex);
                    }
                    throw new InvalidOperationException($"Failed to update cost with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> DeleteCost(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
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
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException($"Failed to delete cost with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        private async Task CheckBudgetLimit(int fiscalYear, double newAmount, int? excludeCostId = null)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();

                try
                {
                    // Tính tổng remaining_amount từ tất cả budgets cho fiscal_year
                    var sqlBudget = @"
                        SELECT COALESCE(SUM(remaining_amount), 0)
                        FROM budgets
                        WHERE fiscal_year = @fiscalYear";
                    double totalRemainingAmount;
                    using (var cmd = new NpgsqlCommand(sqlBudget, _connection))
                    {
                        cmd.Parameters.AddWithValue("@fiscalYear", fiscalYear);
                        var result = await cmd.ExecuteScalarAsync();
                        totalRemainingAmount = Convert.ToDouble(result ?? 0);
                    }

                    // Kiểm tra nếu tổng remaining_amount không đủ để chi trả newAmount
                    if (totalRemainingAmount < newAmount)
                    {
                        throw new InvalidOperationException(
                            $"Tổng tiền quỹ còn lại ({totalRemainingAmount}) không đủ cho số tiền cần tiêu ({newAmount}) trong năm {fiscalYear}.");
                    }
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        // Helper method
        private void ValidateRequest(CostsRequest entity)
        {
            if (entity.task_id <= 0)
            {
                throw new ArgumentException("Task ID must be a positive integer.");
            }
            if (string.IsNullOrWhiteSpace(entity.cost_type))
            {
                throw new ArgumentException("Cost type cannot be empty.");
            }
            if (entity.amount < 0)
            {
                throw new ArgumentException("Amount cannot be negative.");
            }
            // description có thể NULL theo DB, nên không kiểm tra
            if (entity.date_incurred == default(DateTime))
            {
                throw new ArgumentException("Date incurred must be provided.");
            }
        }
    }
}