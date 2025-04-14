using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class UsersRequest
    {
        [Required]
        public string username { get; set; } = string.Empty;
        [Required]
        public string password_hash { get; set; } = string.Empty;
        [Required]
        public string full_name { get; set; } = string.Empty;
        [Required]
        public string email { get; set; } = string.Empty;
        [Required]
        [AllowedValues("admin", "manager", "technician", "inspector")]
        public string role { get; set; } = string.Empty;
        public string image_url {  get; set; } = string.Empty ;
    }
}
