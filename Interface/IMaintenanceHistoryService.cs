using Road_Infrastructure_Asset_Management_2.Model.Response;
using Road_Infrastructure_Asset_Management_2.Model.Request;

namespace Road_Infrastructure_Asset_Management_2.Interface
{
    public interface IMaintenanceHistoryService
    {
        Task<IEnumerable<MaintenanceHistoryResponse>> GetAllMaintenanceHistories();
        Task<IEnumerable<MaintenanceHistoryResponse>> GetMaintenanceHistoryByAssetId(int id);
        Task<(IEnumerable<MaintenanceHistoryResponse>, int)> GetPagedMaintenanceHistoryByAssetId(int id, int currentPage, int pageSize , string searchTerm , int searchField);
        Task<MaintenanceHistoryResponse?> GetMaintenanceHistoryById(int id);
        Task<MaintenanceHistoryResponse?> CreateMaintenanceHistory(MaintenanceHistoryRequest entity);
        Task<MaintenanceHistoryResponse?> UpdateMaintenanceHistory(int id, MaintenanceHistoryRequest entity);
        Task<bool> DeleteMaintenanceHistory(int id);
    }
}
