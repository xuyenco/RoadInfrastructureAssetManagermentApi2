using Npgsql;
using Road_Infrastructure_Asset_Management_2.Interface;
using Road_Infrastructure_Asset_Management_2.Model.Report;
using Road_Infrastructure_Asset_Management_2.Model.Response;

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

        public async Task<IEnumerable<AssetStatusReport>> GetAssetStatusReport()
        {
            var reports = new List<AssetStatusReport>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                SELECT 
                    ac.category_name,
                    COUNT(CASE WHEN a.asset_status = 'in_use' THEN 1 END) AS in_use_count,
                    COUNT(CASE WHEN a.asset_status = 'damaged_not_in_use' THEN 1 END) AS damaged_count
                FROM 
                    assets a
                JOIN 
                    asset_categories ac ON a.category_id = ac.category_id
                GROUP BY 
                    ac.category_name
                ORDER BY 
                    ac.category_name";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var report = new AssetStatusReport
                            {
                                category_name = reader.GetString(reader.GetOrdinal("category_name")),
                                in_use_count = reader.GetInt32(reader.GetOrdinal("in_use_count")),
                                damaged_count = reader.GetInt32(reader.GetOrdinal("damaged_count"))
                            };
                            reports.Add(report);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} asset status reports successfully", reports.Count);
                    return reports;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve asset status reports from database");
                    throw new InvalidOperationException("Failed to retrieve asset status reports from database.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<IncidentDistributionReport>> GetIncidentDistributionReport()
        {
            var reports = new List<IncidentDistributionReport>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                WITH route_counts AS (
                    SELECT 
                        route,
                        COUNT(*) AS incident_count
                    FROM 
                        incidents
                    WHERE 
                        route IS NOT NULL
                    GROUP BY 
                        route
                ),
                ranked_routes AS (
                    SELECT 
                        route,
                        incident_count,
                        ROW_NUMBER() OVER (ORDER BY incident_count DESC) AS rank
                    FROM 
                        route_counts
                ),
                top_routes AS (
                    SELECT 
                        route,
                        incident_count
                    FROM 
                        ranked_routes
                    WHERE 
                        rank <= 10
                ),
                other_routes AS (
                    SELECT 
                        'Other' AS route,
                        COALESCE(SUM(incident_count), 0) AS incident_count
                    FROM 
                        ranked_routes
                    WHERE 
                        rank > 10
                )
                SELECT 
                    route,
                    incident_count
                FROM 
                    top_routes
                UNION ALL
                SELECT 
                    route,
                    incident_count
                FROM 
                    other_routes
                WHERE 
                    incident_count > 0
                ORDER BY 
                    incident_count DESC, route";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var report = new IncidentDistributionReport
                            {
                                route = reader.GetString(reader.GetOrdinal("route")),
                                incident_count = reader.GetInt32(reader.GetOrdinal("incident_count"))
                            };
                            reports.Add(report);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} incident distribution reports successfully", reports.Count);
                    return reports;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve incident distribution reports from database");
                    throw new InvalidOperationException("Failed to retrieve incident distribution reports from database.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<TaskPerformanceReport>> GetTaskPerformanceReport()
        {
            var reports = new List<TaskPerformanceReport>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                SELECT 
                    u.department_company_unit,
                    COUNT(t.task_id) AS task_count,
                    AVG((t.end_date - t.start_date) / 3600.0) AS avg_hours_to_complete
                FROM 
                    tasks t
                JOIN 
                    users u ON t.execution_unit_id = u.user_id
                WHERE 
                    t.start_date IS NOT NULL
                    AND t.end_date IS NOT NULL
                    AND t.end_date >= t.start_date
                GROUP BY 
                    u.department_company_unit
                ORDER BY 
                    task_count DESC";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var report = new TaskPerformanceReport
                            {
                                department_company_unit = reader.GetString(reader.GetOrdinal("department_company_unit")),
                                task_count = reader.GetInt32(reader.GetOrdinal("task_count")),
                                avg_hours_to_complete = reader.IsDBNull(reader.GetOrdinal("avg_hours_to_complete"))
                                    ? 0.0
                                    : reader.GetDouble(reader.GetOrdinal("avg_hours_to_complete"))
                            };
                            reports.Add(report);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} task performance reports successfully", reports.Count);
                    return reports;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve task performance reports from database");
                    throw new InvalidOperationException("Failed to retrieve task performance reports from database.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<IncidentTaskTrendReport>> GetIncidentTaskTrendReport()
        {
            var reports = new List<IncidentTaskTrendReport>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                SELECT 
                    DATE_TRUNC('month', COALESCE(i.created_at, t.created_at)) AS month,
                    COUNT(i.incident_id) AS incident_count,
                    COUNT(t.task_id) AS task_count,
                    COALESCE(t.status, 'N/A') AS task_status,
                    COUNT(t.task_id) FILTER (WHERE t.status = 'completed') AS completed_task_count
                FROM 
                    incidents i
                FULL OUTER JOIN 
                    tasks t ON DATE_TRUNC('month', i.created_at) = DATE_TRUNC('month', t.created_at)
                GROUP BY 
                    DATE_TRUNC('month', COALESCE(i.created_at, t.created_at)), t.status
                ORDER BY 
                    month";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var report = new IncidentTaskTrendReport
                            {
                                month = reader.GetDateTime(reader.GetOrdinal("month")),
                                incident_count = reader.GetInt32(reader.GetOrdinal("incident_count")),
                                task_count = reader.GetInt32(reader.GetOrdinal("task_count")),
                                task_status = reader.GetString(reader.GetOrdinal("task_status")),
                                completed_task_count = reader.GetInt32(reader.GetOrdinal("completed_task_count"))
                            };
                            reports.Add(report);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} incident and task trend reports successfully", reports.Count);
                    return reports;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve incident and task trend reports from database");
                    throw new InvalidOperationException("Failed to retrieve incident and task trend reports from database.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }
        public async Task<IEnumerable<MaintenanceFrequencyReport>> GetMaintenanceFrequencyReport()
        {
            var reports = new List<MaintenanceFrequencyReport>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                SELECT 
                    a.asset_id,
                    a.asset_name,
                    COUNT(m.maintenance_id) AS maintenance_count,
                    MAX(t.end_date) AS latest_maintenance_date,
                    COALESCE(t.status, 'N/A') AS latest_maintenance_status
                FROM 
                    assets a
                LEFT JOIN 
                    maintenance_history m ON a.asset_id = m.asset_id
                LEFT JOIN 
                    tasks t ON m.task_id = t.task_id
                GROUP BY 
                    a.asset_id, a.asset_name, t.status
                ORDER BY 
                    maintenance_count DESC, latest_maintenance_date DESC";

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var report = new MaintenanceFrequencyReport
                            {
                                asset_id = reader.GetInt32(reader.GetOrdinal("asset_id")),
                                asset_name = reader.GetString(reader.GetOrdinal("asset_name")),
                                maintenance_count = reader.GetInt32(reader.GetOrdinal("maintenance_count")),
                                latest_maintenance_date = reader.IsDBNull(reader.GetOrdinal("latest_maintenance_date"))
                                    ? null
                                    : reader.GetDateTime(reader.GetOrdinal("latest_maintenance_date")),
                                latest_maintenance_status = reader.GetString(reader.GetOrdinal("latest_maintenance_status"))
                            };
                            reports.Add(report);
                        }
                    }
                    _logger.LogInformation("Retrieved {Count} maintenance frequency reports successfully", reports.Count);
                    return reports;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Failed to retrieve maintenance frequency reports from database");
                    throw new InvalidOperationException("Failed to retrieve maintenance frequency reports from database.", ex);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}