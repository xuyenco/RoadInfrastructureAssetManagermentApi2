﻿using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Interface
{
    public interface IIncidentImageService
    {
        Task<IEnumerable<IncidentImageResponse>> GetAllIncidentImages();
        Task<IncidentImageResponse?> GetIncidentImageById(int id);
        Task<IEnumerable<IncidentImageResponse>> GetAllIncidentImagesByIncidentId(int incidentId);
        Task<IncidentImageResponse?> CreateIncidentImage(IncidentImageRequest entity);
        Task<IncidentImageResponse?> UpdateIncidentImage(int id, IncidentImageRequest entity);
        Task<bool> DeleteIncidentImage(int id);
        Task<bool> DeleteIncidentImageByIncidentId(int id);
    }
}
