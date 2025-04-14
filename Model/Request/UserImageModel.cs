using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management_2.Model.Request
{
    public class UserImageModel
    {
        [Required]
        public string username { get; set; } = string.Empty;
        public string password_hash { get; set; } = string.Empty;
        [Required]
        public string full_name { get; set; } = string.Empty;
        [Required]
        public string email { get; set; } = string.Empty;
        [Required]
        [AllowedValues("admin", "manager", "technician", "inspector")]
        public string role { get; set; } = string.Empty;
        public IFormFile image { get; set; }
    }
}
