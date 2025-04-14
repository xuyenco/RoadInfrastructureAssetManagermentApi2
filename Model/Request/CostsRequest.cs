using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class CostsRequest
    {
        [Required]
        public int task_id { get; set; }
        [Required]
        public string cost_type { get; set; } = string.Empty;
        [Required]
        public double amount { get; set; }
        [Required]
        public string description { get; set; } = string.Empty;
        [Required]
        public DateTime? date_incurred { get; set; }
    }
}
