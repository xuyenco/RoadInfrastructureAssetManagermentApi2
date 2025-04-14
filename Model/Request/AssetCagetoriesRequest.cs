using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class AssetCagetoriesRequest
    {
        [Required]
        public string cagetory_name { get; set; }
        [Required]
        [AllowedValues("point", "line", "polygon")]
        public string geometry_type { get; set; }
        [Required]
        public JObject attributes_schema { get; set; } // JSON object
        [Required]
        public JArray lifecycle_stages { get; set; }   // JSON array
        public string marker_url { get; set; } = "";
    }
}