﻿using Road_Infrastructure_Asset_Management_2.Model.Geometry;

namespace Road_Infrastructure_Asset_Management_2.Model.Response
{
    public class IncidentsResponse
    {
        public int incident_id { get; set; }
        public string address { get; set; }
        public string incident_type { get; set; }
        public GeoJsonGeometry geometry { get; set; } = new GeoJsonGeometry();
        public string route { get; set; }
        public string severity_level { get; set; }
        public string damage_level { get; set; }
        public string processing_status { get; set; }
        public int? task_id { get; set; }
        public string description { get; set; }
        public DateTime? created_at { get; set; }
    }
}