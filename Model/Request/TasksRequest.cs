using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class TasksRequest
    {
        [Required]
        public int asset_id { get; set; }
        [Required]
        public int assigned_to { get; set; }
        [Required]
        public string task_type { get; set; } = string.Empty;
        [Required]
        public string description { get; set; } = string.Empty;
        [Required]
        [AllowedValues("low","medium","high")]
        public string priority { get; set; } = string.Empty;
        [Required]
        [AllowedValues("pending", "in-progress","compeleted", "canelled")]
        public string status { get; set; } = string.Empty;
        [Required]
        public DateTime due_date { get; set; }
    }
}
