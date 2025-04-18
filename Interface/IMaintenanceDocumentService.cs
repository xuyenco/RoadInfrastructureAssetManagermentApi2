using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Interface
{
    public interface IMaintenanceDocumentService
    {
        Task<IEnumerable<MaintenanceDocumentResponse>> GetAllMaintenanceDocuments();
        Task<IEnumerable<MaintenanceDocumentResponse>> GetMaintenanceDocumentByMaintenanceId(int id);
        Task<MaintenanceDocumentResponse?> GetMaintenanceDocumentById(int id);
        Task<MaintenanceDocumentResponse?> CreateMaintenanceDocument(MaintenanceDocumentRequest entity);
        Task<MaintenanceDocumentResponse?> UpdateMaintenanceDocument(int id, MaintenanceDocumentRequest entity);
        Task<bool> DeleteMaintenanceDocument(int id);
        Task<bool> DeleteMaintenanceDocumentByMaintenanceId(int id);
    }
}
