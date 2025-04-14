using Road_Infrastructure_Asset_Management.Model.Geometry;
using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class IncidentsRequest
    {
        public int asset_id { get; set; } // optional
        [Required]
        public int reported_by { get; set; }
        [Required]
        public string incident_type { get; set; } = string.Empty;
        [Required]
        public string description { get; set; } = string.Empty;
        [Required]
        public GeoJsonGeometry location { get; set; } = new GeoJsonGeometry();
        [Required]
        [AllowedValues("low", "medium", "high", "critical")] 
        public string priority { get; set; } = string.Empty;
        [Required]
        [AllowedValues("reported", "under review", "resolved", "closed")]
        public string status { get; set; } = string.Empty;
        public DateTime? resolved_at { get; set; }
        public string notes { get; set; } = string.Empty;
    }
}
