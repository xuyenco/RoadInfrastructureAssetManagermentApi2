using Newtonsoft.Json.Linq;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management_2.Model.ImageUpload
{
    public class AssetCagetoryImageUploadRequest
    {
        [Required]
        public string category_name { get; set; }
        [Required]
        [AllowedValues("point", "linestring", "polygon")]
        public string geometry_type { get; set; }
        [Required]
        public string attribute_schema { get; set; } // JSON object
        public IFormFile sample_image { get; set; }
        public IFormFile icon { get; set; }
    }
}
