using Newtonsoft.Json.Linq;
using Road_Infrastructure_Asset_Management.Model.Geometry;
using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class AssetsRequest
    {
        [Required]
        public int cagetory_id { get; set; }
        [Required]
        public GeoJsonGeometry geometry { get; set; } = new GeoJsonGeometry();
        [Required]
        public JObject attributes { get; set; }
        [Required]
        public string lifecycle_stage { get; set; } = string.Empty ;
        [Required]
        public DateTime? installation_date { get; set; }
        [Required]
        public int expected_lifetime { get; set; }
        [Required]
        public string condition { get; set; } = string.Empty;
        public DateTime? last_inspection_date { get; set; }
    }
}
