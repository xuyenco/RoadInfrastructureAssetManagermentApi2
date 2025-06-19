using Road_Infrastructure_Asset_Management_2.Model.Report;

namespace Road_Infrastructure_Asset_Management_2.Interface
{
    public interface IReportService
    {
        Task<IEnumerable<AssetStatusReport>> GetAssetDistributedByCondition();
        Task<IEnumerable<IncidentTaskTrendReport>> GetIncidentsOverTime();
        Task<IEnumerable<IncidentDistributionReport>> GetIncidentTypeDistribution();
        Task<IEnumerable<MaintenanceFrequencyReport>> GetMaintenanceFrequencyReport();
        Task<IEnumerable<TaskPerformanceReport>> GetTaskStatusDistribution();
    }
}
