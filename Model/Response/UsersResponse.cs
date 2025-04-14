namespace Road_Infrastructure_Asset_Management.Model.Response
{
    public class UsersResponse
    {
        public int user_id { get; set; }
        public string username { get; set; }
        public string full_name { get; set; }
        public string email { get; set; }
        public string role { get; set; }
        public DateTime created_at { get; set; }
        public string? refresh_token { get; set; } // Thêm refresh token
        public DateTime? refresh_token_expiry { get; set; } // Thời gian hết hạn
        public string? image_url { get; set; }
    }
}
