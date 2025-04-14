using Newtonsoft.Json.Linq;
using Road_Infrastructure_Asset_Management.Model.Request;
using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management.Model.ImageUpload
{
    public class AssetCagetoryImageModel
    {
        [Required]
        public string cagetory_name { get; set; }
        [Required]
        [AllowedValues("point", "line", "polygon")]
        public string geometry_type { get; set; }
        [Required]
        public string attributes_schema { get; set; } // JSON object
        [Required]
        public string lifecycle_stages { get; set; }   // JSON array
        public IFormFile marker { get; set; }
    }
}
