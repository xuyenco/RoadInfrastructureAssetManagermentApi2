using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Interface
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
