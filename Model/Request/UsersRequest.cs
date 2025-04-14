using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management_2.Model.Request
{
    public class UsersRequest
    {
        [Required]
        public string username { get; set; } = string.Empty;
        [Required]
        public string password { get; set; } = string.Empty;
        public string full_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        [Required]
        [AllowedValues("admin", "manager", "technician", "inspector", "supervisor")]
        public string role { get; set; } = string.Empty;
        public string department_company_unit { get; set; } = string.Empty;
    }
}
