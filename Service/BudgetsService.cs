using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class BudgetsService : IBudgetsService
    {
        private readonly string _connectionString;
        private readonly ILogger<BudgetsService> _logger;

        public BudgetsService(string connectionString, ILogger<BudgetsService> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<IEnumerable<BudgetsResponse>> GetAllBudgets()
        {
            var budgets = new List<BudgetsResponse>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM budgets";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var budget = new BudgetsResponse
                            {
                                budget_id = reader.GetInt32(reader.GetOrdinal("budget_id")),
                                cagetory_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                fiscal_year = reader.GetInt32(reader.GetOrdinal("fiscal_year")),
                                total_amount = reader.GetDouble(reader.GetOrdinal("total_amount")),
                                allocated_amount = reader.GetDouble(reader.GetOrdinal("allocated_amount")),
                                remaining_amount = reader.GetDouble(reader.GetOrdinal("remaining_amount")),
                                created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            };
                            budgets.Add(budget);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} budgets successfully", budgets.Count);
                    return budgets;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve budgets from database");
                    throw new InvalidOperationException("Failed to retrieve budgets from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<BudgetsResponse?> GetBudgetById(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM budgets WHERE budget_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var budget = new BudgetsResponse
                                {
                                    budget_id = reader.GetInt32(reader.GetOrdinal("budget_id")),
                                    cagetory_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                    fiscal_year = reader.GetInt32(reader.GetOrdinal("fiscal_year")),
                                    total_amount = reader.GetDouble(reader.GetOrdinal("total_amount")),
                                    allocated_amount = reader.GetDouble(reader.GetOrdinal("allocated_amount")),
                                    remaining_amount = reader.GetDouble(reader.GetOrdinal("remaining_amount")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                _logger.LogInformation("Retrieved budget with ID {BudgetId} successfully", id);
                                return budget;
                            }
                            _logger.LogWarning("Budget with ID {BudgetId} not found", id);
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve budget with ID {BudgetId}", id);
                    throw new InvalidOperationException($"Failed to retrieve budget with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<BudgetsResponse?> CreateBudget(BudgetsRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                INSERT INTO budgets 
                (category_id, fiscal_year, total_amount, allocated_amount, remaining_amount)
                VALUES (@category_id, @fiscal_year, @total_amount, @allocated_amount, @remaining_amount)
                RETURNING budget_id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@category_id", entity.cagetory_id);
                        cmd.Parameters.AddWithValue("@fiscal_year", entity.fiscal_year);
                        cmd.Parameters.AddWithValue("@total_amount", entity.total_amount);
                        cmd.Parameters.AddWithValue("@allocated_amount", entity.allocated_amount);
                        cmd.Parameters.AddWithValue("@remaining_amount", entity.remaining_amount);
                        var newId = (int)(await cmd.ExecuteScalarAsync())!;
                        _logger.LogInformation("Created budget with ID {BudgetId} successfully", newId);
                        return await GetBudgetById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to create budget: Invalid category ID {CategoryId}", entity.cagetory_id);
                        throw new InvalidOperationException($"Category ID {entity.cagetory_id} does not exist.", ex);
                    }
                    _logger.LogError(ex, "Failed to create budget");
                    throw new InvalidOperationException("Failed to create budget.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<BudgetsResponse?> UpdateBudget(int id, BudgetsRequest entity)
        {
            ValidateRequest(entity);

            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"
                UPDATE budgets SET
                    category_id = @category_id,
                    fiscal_year = @fiscal_year,
                    total_amount = @total_amount,
                    allocated_amount = @allocated_amount,
                    remaining_amount = @remaining_amount
                WHERE budget_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@category_id", entity.cagetory_id);
                        cmd.Parameters.AddWithValue("@fiscal_year", entity.fiscal_year);
                        cmd.Parameters.AddWithValue("@total_amount", entity.total_amount);
                        cmd.Parameters.AddWithValue("@allocated_amount", entity.allocated_amount);
                        cmd.Parameters.AddWithValue("@remaining_amount", entity.remaining_amount);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Updated budget with ID {BudgetId} successfully", id);
                            return await GetBudgetById(id);
                        }
                        _logger.LogWarning("Budget with ID {BudgetId} not found for update", id);
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to update budget with ID {BudgetId}: Invalid category ID {CategoryId}", id, entity.cagetory_id); // Log lỗi khóa ngoại
                        throw new InvalidOperationException($"Category ID {entity.cagetory_id} does not exist.", ex);
                    }
                    _logger.LogError(ex, "Failed to update budget with ID {BudgetId}", id);
                    throw new InvalidOperationException($"Failed to update budget with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> DeleteBudget(int id)
        {
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "DELETE FROM budgets WHERE budget_id = @id";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            _logger.LogInformation("Deleted budget with ID {BudgetId} successfully", id);
                            return true;
                        }
                        _logger.LogWarning("Budget with ID {BudgetId} not found for deletion", id);
                        return false;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        _logger.LogError(ex, "Failed to delete budget with ID {BudgetId}: Referenced by other records", id);
                        throw new InvalidOperationException($"Cannot delete budget with ID {id} because it is referenced by other records.", ex);
                    }
                    _logger.LogError(ex, "Failed to delete budget with ID {BudgetId}", id);
                    throw new InvalidOperationException($"Failed to delete budget with ID {id}.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        // Helper method
        private void ValidateRequest(BudgetsRequest entity)
        {
            if (entity.cagetory_id <= 0)
            {
                _logger.LogWarning("Validation failed: Category ID must be a positive integer");
                throw new ArgumentException("Category ID must be a positive integer.");
            }
            if (entity.fiscal_year <= 0)
            {
                _logger.LogWarning("Validation failed: Fiscal year must be a positive integer");
                throw new ArgumentException("Fiscal year must be a positive integer.");
            }
            if (entity.total_amount < 0)
            {
                _logger.LogWarning("Validation failed: Total amount cannot be negative");
                throw new ArgumentException("Total amount cannot be negative.");
            }
            if (entity.allocated_amount < 0)
            {
                _logger.LogWarning("Validation failed: Allocated amount cannot be negative");
                throw new ArgumentException("Allocated amount cannot be negative.");
            }
            if (entity.remaining_amount < 0)
            {
                _logger.LogWarning("Validation failed: Remaining amount cannot be negative");
                throw new ArgumentException("Remaining amount cannot be negative.");
            }
        }
    }
}