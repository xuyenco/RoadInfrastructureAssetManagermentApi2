using Road_Infrastructure_Asset_Management_2.Model.Report;

namespace Road_Infrastructure_Asset_Management_2.Interface
{
    public interface IReportService
    {
        Task<IEnumerable<AssetStatusReport>> GetAssetStatusReport();
        Task<IEnumerable<IncidentTaskTrendReport>> GetIncidentTaskTrendReport();
        Task<IEnumerable<IncidentDistributionReport>> GetIncidentDistributionReport();
        Task<IEnumerable<MaintenanceFrequencyReport>> GetMaintenanceFrequencyReport();
        Task<IEnumerable<TaskPerformanceReport>> GetTaskPerformanceReport();
    }
}
