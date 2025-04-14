using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class IncidentHistoryRequest
    {
        [Required]
        public int incident_id { get; set; }
        [Required]
        public int task_id { get; set; }
        [Required]
        public int changed_by { get; set; }
        [Required]
        public string old_status { get; set; } = string.Empty;
        [Required]
        public string new_status { get; set; } = string.Empty;
        [Required]
        public string change_description { get; set; } = string.Empty;
    }
}
