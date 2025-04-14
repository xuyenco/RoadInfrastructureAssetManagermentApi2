using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management_2.Model.Request
{
    public class BudgetsRequest
    {
        [Required]
        public int cagetory_id { get; set; }
        [Required]
        public int fiscal_year { get; set; }
        [Required]
        public double total_amount { get; set; }
        [Required]
        public double allocated_amount { get; set; }
        [Required]
        public double remaining_amount { get; set; }
    }
}
