using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace Road_Infrastructure_Asset_Management.Interface
{
    public interface IIncidentsService
    {
        Task<IEnumerable<IncidentsResponse>> GetAllIncidents();
        Task<IncidentsResponse?> GetIncidentById(int id);
        Task<IncidentsResponse?> CreateIncident(IncidentsRequest entity);
        Task<IncidentsResponse?> UpdateIncident(int id, IncidentsRequest entity);
        Task<bool> DeleteIncident(int id);
    }
}
