﻿namespace Road_Infrastructure_Asset_Management_2.Model.ImageUpload
{
    public class IncidentImageUploadRequest
    {
        public int incident_id { get; set; }
        public IFormFile image { get; set; }
    }
}
