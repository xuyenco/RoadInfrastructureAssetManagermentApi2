using Newtonsoft.Json.Linq;

namespace Road_Infrastructure_Asset_Management_2.Model.Response
{
    public class AssetCategoriesResponse
    {
        public int category_id {  get; set; }
        public string category_name { get; set; }
        public string geometry_type { get; set; }
        public JObject attribute_schema { get; set; } // JSON object
        public DateTime? created_at { get; set; }
        public string sample_image { get; set; }
        public string sample_image_name { get; set; }
        public string sample_image_public_id { get; set; }
        public string icon_url { get; set; }
        public string icon_public_id { get; set; }
    }
}
