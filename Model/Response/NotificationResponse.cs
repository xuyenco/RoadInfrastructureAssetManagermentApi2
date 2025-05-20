namespace Road_Infrastructure_Asset_Management_2.Model.Response
{
    public class NotificationResponse
    {
        public int notification_id {  get; set; }
        public int user_id { get; set; }
        public int task_id { get; set; }
        public string message { get; set; }
        public bool is_read { get; set; }
        public DateTime created_at { get; set; }
        public string notification_type { get; set; }
    }
}
