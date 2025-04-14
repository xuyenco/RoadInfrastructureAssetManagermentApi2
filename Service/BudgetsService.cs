using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class BudgetsService : IBudgetsService
    {
        private readonly string _connectionString;

        public BudgetsService(string connectionString)
        {
            _connectionString = connectionString;
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
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException("Failed to retrieve budgets from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
                return budgets;
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
                                return new BudgetsResponse
                                {
                                    budget_id = reader.GetInt32(reader.GetOrdinal("budget_id")),
                                    cagetory_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                    fiscal_year = reader.GetInt32(reader.GetOrdinal("fiscal_year")),
                                    total_amount = reader.GetDouble(reader.GetOrdinal("total_amount")),
                                    allocated_amount = reader.GetDouble(reader.GetOrdinal("allocated_amount")),
                                    remaining_amount = reader.GetDouble(reader.GetOrdinal("remaining_amount")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                            }
                            return null;
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
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
                        return await GetBudgetById(newId);
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Category ID {entity.cagetory_id} does not exist.", ex);
                    }
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
                            return await GetBudgetById(id);
                        }
                        return null;
                    }
                }
                catch (NpgsqlException ex)
                {
                    if (ex.SqlState == "23503") // Foreign key violation
                    {
                        throw new InvalidOperationException($"Category ID {entity.cagetory_id} does not exist.", ex);
                    }
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
                        return affectedRows > 0;
                    }
                }
                catch (NpgsqlException ex)
                {
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
                throw new ArgumentException("Category ID must be a positive integer.");
            }
            if (entity.fiscal_year <= 0)
            {
                throw new ArgumentException("Fiscal year must be a positive integer.");
            }
            if (entity.total_amount < 0)
            {
                throw new ArgumentException("Total amount cannot be negative.");
            }
            if (entity.allocated_amount < 0)
            {
                throw new ArgumentException("Allocated amount cannot be negative.");
            }
            if (entity.remaining_amount < 0)
            {
                throw new ArgumentException("Remaining amount cannot be negative.");
            }
        }


    }
}