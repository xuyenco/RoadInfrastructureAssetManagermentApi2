using Road_Infrastructure_Asset_Management_2.Model.Report;

namespace Road_Infrastructure_Asset_Management_2.Interface
{
    public interface IReportService
    {
        Task<IEnumerable<TaskStatusDistribution>> GetTaskStatusDistributions();
        Task<IEnumerable<IncidentTypeDistribution>> GetIncidentTypeDistributions();
        Task<IEnumerable<IncidentsOverTime>> GetIncidentsOverTime();
        Task<IEnumerable<BudgetAndCost>> GetBudgetAndCosts();
        Task<IEnumerable<AssetDistributionByCategory>> GetAssetDistributionByCategories();
        Task<IEnumerable<AssetDistributedByCondition>> GetAssetDistributedByCondition();
    }
}
