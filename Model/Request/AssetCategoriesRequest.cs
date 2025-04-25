using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management_2.Model.Request
{
    public class AssetCategoriesRequest
    {
        [Required]
        public string category_name { get; set; }
        [Required]
        [AllowedValues("point", "linestring", "polygon")]
        public string geometry_type { get; set; }
        [Required]
        public JObject attribute_schema { get; set; } // JSON object
        public string sample_image { get; set; }
        public string sample_image_name { get; set; }
        public string sample_image_public_id { get; set; }
        public string icon_url { get; set; }
        public string icon_public_id { get; set; }
    }
}