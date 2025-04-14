using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace Road_Infrastructure_Asset_Management.Interface
{
    public interface IIncidentHistoryService
    {
        Task<IEnumerable<IncidentHistoryResponse>> GetAllIncidentHistory();
        Task<IncidentHistoryResponse?> GetIncidentHistoryById(int id);
        Task<IEnumerable<IncidentHistoryResponse>> GetIncidentHistoryByIncidentID(int id);
        Task<IncidentHistoryResponse?> CreateIncidentHistory(IncidentHistoryRequest entity);
        Task<IncidentHistoryResponse?> UpdateIncidentHistory(int id, IncidentHistoryRequest entity);
        Task<bool> DeleteIncidentHistory(int id);
    }
}
