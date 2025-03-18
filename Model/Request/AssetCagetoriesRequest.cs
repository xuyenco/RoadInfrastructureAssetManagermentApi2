using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class AssetCagetoriesRequest
    {
        public string cagetory_name { get; set; }
        public string geometry_type { get; set; }
        public JObject attributes_schema { get; set; } // JSON object
        public JArray lifecycle_stages { get; set; }   // JSON array
    }
}
