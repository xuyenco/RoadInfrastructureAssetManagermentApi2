using Road_Infrastructure_Asset_Management_2.Model.Response;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Interface
{
    public interface IMaintenanceHistoryService
    {
        Task<IEnumerable<MaintenanceHistoryResponse>> GetAllMaintenanceHistories();
        Task<MaintenanceHistoryResponse?> GetMaintenanceHistoryById(int id);
        Task<MaintenanceHistoryResponse?> CreateMaintenanceHistory(MaintenanceHistoryRequest entity);
        Task<MaintenanceHistoryResponse?> UpdateMaintenanceHistory(int id, MaintenanceHistoryRequest entity);
        Task<bool> DeleteMaintenanceHistory(int id);
    }
}
