using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace Road_Infrastructure_Asset_Management.Interface
{
    public interface IIncidentImageService
    {
        Task<IEnumerable<IncidentImageResponse>> GetAllIncidentImages();
        Task<IncidentImageResponse?> GetIncidentImageById(int id);
        Task<IEnumerable<IncidentImageResponse>> GetAllIncidentImagesByIncidentId(int incidentId);
        Task<IncidentImageResponse?> CreateIncidentImage(IncidentImageRequest entity);
        Task<IncidentImageResponse?> UpdateIncidentImage(int id, IncidentImageRequest entity);
        Task<bool> DeleteIncidentImage(int id);
    }
}
