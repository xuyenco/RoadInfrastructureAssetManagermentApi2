namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class IncidentImageUploadRequest
    {
        public int incident_id { get; set; }
        public IFormFile image { get; set; }
    }
}
