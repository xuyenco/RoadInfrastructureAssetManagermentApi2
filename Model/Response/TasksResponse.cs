﻿using Road_Infrastructure_Asset_Management_2.Model.Geometry;

namespace Road_Infrastructure_Asset_Management_2.Model.Response
{
    public class TasksResponse
    {
        public int task_id { get; set; }
        public string task_type { get; set; }
        public string work_volume { get; set; }
        public string status { get; set; }
        public string address { get; set; }
        public GeoJsonGeometry geometry { get; set; } = new GeoJsonGeometry();
        public DateTime? start_date { get; set; }
        public DateTime? end_date { get; set; }
        public int? execution_unit_id { get; set; }
        public int? supervisor_id { get; set; }
        public DateTime? created_at { get; set; }
    }
}