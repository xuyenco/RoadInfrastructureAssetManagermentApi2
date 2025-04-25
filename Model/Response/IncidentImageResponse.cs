namespace Road_Infrastructure_Asset_Management_2.Model.Response
{
    public class IncidentImageResponse
    {
        public int incident_image_id { get; set; }
        public int incident_id { get; set; }
        public string image_url { get; set; }
        public string image_public_id { get; set; }
        public string image_name { get; set; }
        public DateTime created_at { get; set; }
    }
}
