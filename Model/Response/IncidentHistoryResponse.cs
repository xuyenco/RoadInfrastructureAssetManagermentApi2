﻿namespace Road_Infrastructure_Asset_Management.Model.Response
{
    public class IncidentHistoryResponse
    {
        public int history_id {  get; set; }
        public int incident_id { get; set; }
        public int task_id { get; set; }
        public int changed_by { get; set; }
        public string old_status { get; set; } = string.Empty;
        public string new_status { get; set; } = string.Empty;
        public string change_description { get; set; } = string.Empty;
        public DateTime? changed_at { get; set; }
    }
}
