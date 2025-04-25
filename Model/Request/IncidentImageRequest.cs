namespace Road_Infrastructure_Asset_Management_2.Model.Request
{
    public class IncidentImageRequest
    {
        public int incident_id { get; set; }
        public string image_url { get; set; }
        public string image_public_id { get; set; }
        public string image_name { get; set; }
    }
}
