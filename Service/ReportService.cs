using Microsoft.Extensions.Logging; 
using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Report;

namespace Road_Infrastructure_Asset_Management_2.Service
{
    public class ReportService : IReportService
    {
        private readonly string _connectionString;
        private readonly ILogger<ReportService> _logger; 

        public ReportService(string connectionString, ILogger<ReportService> logger) 
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<IEnumerable<TaskStatusDistribution>> GetTaskStatusDistributions()
        {
            var TaskStatusDistributions = new List<TaskStatusDistribution>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT status, (count (task_id)) AS count FROM tasks GROUP BY status";
                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var TaskStatusDistribution = new TaskStatusDistribution
                                {
                                    count = reader.GetInt32(reader.GetOrdinal("count")),
                                    status = reader.GetString(reader.GetOrdinal("status")),
                                };
                                TaskStatusDistributions.Add(TaskStatusDistribution);
                            }
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} task status distributions successfully", TaskStatusDistributions.Count);
                    return TaskStatusDistributions;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve Task Status Distributions Report from database"); 
                    throw new InvalidOperationException("Failed to retrieve Task Status Distributions Report from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<IncidentTypeDistribution>> GetIncidentTypeDistributions()
        {
            var IncidentTypeDistributions = new List<IncidentTypeDistribution>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT incident_type, COUNT( incident_id) AS count FROM incidents GROUP BY incident_type";
                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var IncidentTypeDistribution = new IncidentTypeDistribution
                                {
                                    count = reader.GetInt32(reader.GetOrdinal("count")),
                                    incident_type = reader.GetString(reader.GetOrdinal("incident_type")),
                                };
                                IncidentTypeDistributions.Add(IncidentTypeDistribution);
                            }
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} incident type distributions successfully", IncidentTypeDistributions.Count); 
                    return IncidentTypeDistributions;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve Incident Type Distributions Report from database"); 
                    throw new InvalidOperationException("Failed to retrieve Incident Type Distributions Report from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<IncidentsOverTime>> GetIncidentsOverTime()
        {
            var IncidentsOvertimes = new List<IncidentsOverTime>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = "SELECT EXTRACT(YEAR FROM reported_at) AS year, EXTRACT(MONTH FROM reported_at) AS month, count ( incident_id) as count FROM incidents GROUP BY year,month ORDER BY year,month";
                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var IncidentsOverTime = new IncidentsOverTime
                                {
                                    year = reader.GetInt32(reader.GetOrdinal("year")),
                                    month = reader.GetInt32(reader.GetOrdinal("month")),
                                    count = reader.GetInt32(reader.GetOrdinal("count")),
                                };
                                IncidentsOvertimes.Add(IncidentsOverTime);
                            }
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} incidents over time successfully", IncidentsOvertimes.Count); 
                    return IncidentsOvertimes;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve Incident Over Time Report from database"); 
                    throw new InvalidOperationException("Failed to retrieve Incident Over Time Report from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<BudgetAndCost>> GetBudgetAndCosts()
        {
            var BudgetAndCosts = new List<BudgetAndCost>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"WITH CostsByYear AS (
                                SELECT EXTRACT(YEAR FROM date_incurred) AS year, SUM(amount) AS total_cost
                                FROM costs
                                GROUP BY EXTRACT(YEAR FROM date_incurred)
                            ),
                            BudgetsByYear AS (
                                SELECT fiscal_year AS year, SUM(total_amount) AS total_budget
                                FROM budgets
                                GROUP BY fiscal_year
                            )
                            SELECT 
                                COALESCE(c.year, b.year) AS fiscal_year,
                                COALESCE(c.total_cost, 0) AS total_cost,
                                COALESCE(b.total_budget, 0) AS total_budget
                            FROM CostsByYear c
                            FULL OUTER JOIN BudgetsByYear b ON c.year = b.year
                            ORDER BY fiscal_year;";
                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var BudgetAndCost = new BudgetAndCost
                                {
                                    fiscal_year = reader.GetInt32(reader.GetOrdinal("fiscal_year")),
                                    total_budget = reader.GetInt32(reader.GetOrdinal("total_budget")),
                                    total_cost = reader.GetInt32(reader.GetOrdinal("total_cost")),
                                };
                                BudgetAndCosts.Add(BudgetAndCost);
                            }
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} budget and cost records successfully", BudgetAndCosts.Count);
                    return BudgetAndCosts;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve Budget and Cost Report from database"); 
                    throw new InvalidOperationException("Failed to retrieve Budget and Cost Report from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<AssetDistributionByCategory>> GetAssetDistributionByCategories()
        {
            var AssetDistributionByCategories = new List<AssetDistributionByCategory>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"SELECT category_name, COUNT (asset_id) AS count
                            FROM asset_categories LEFT JOIN assets ON assets.category_id = asset_categories.category_id
                            GROUP BY asset_categories.category_id";
                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var AssetDistributionByCategory = new AssetDistributionByCategory
                                {
                                    category_name = reader.GetString(reader.GetOrdinal("category_name")),
                                    count = reader.GetInt32(reader.GetOrdinal("count")),
                                };
                                AssetDistributionByCategories.Add(AssetDistributionByCategory);
                            }
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} asset distributions by category successfully", AssetDistributionByCategories.Count); 
                    return AssetDistributionByCategories;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve Asset Distributed By Category from database"); 
                    throw new InvalidOperationException("Failed to retrieve Asset Distributed By Category from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<AssetDistributedByCondition>> GetAssetDistributedByCondition()
        {
            var AssetDistributedByConditions = new List<AssetDistributedByCondition>();
            using (var _connection = new NpgsqlConnection(_connectionString))
            {
                await _connection.OpenAsync();
                var sql = @"SELECT condition, COUNT(asset_id) as count
                            FROM assets
                            GROUP BY condition";
                try
                {
                    using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var AssetDistributedByCondition = new AssetDistributedByCondition
                                {
                                    condition = reader.GetString(reader.GetOrdinal("condition")),
                                    count = reader.GetInt32(reader.GetOrdinal("count")),
                                };
                                AssetDistributedByConditions.Add(AssetDistributedByCondition);
                            }
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} asset distributions by condition successfully", AssetDistributedByConditions.Count); 
                    return AssetDistributedByConditions;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve Asset Distributed By Condition from database"); 
                    throw new InvalidOperationException("Failed to retrieve Asset Distributed By Condition from database.", ex);
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
        }
    }
}