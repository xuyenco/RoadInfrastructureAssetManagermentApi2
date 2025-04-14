﻿namespace Road_Infrastructure_Asset_Management_2.Model.Response
{
    public class UsersResponse
    {
        public int user_id { get; set; }
        public string username { get; set; }
        public string full_name { get; set; }
        public string email { get; set; }
        public string role { get; set; }
        public string department_company_unit { get; set; }
        public DateTime created_at { get; set; }
        public string? refresh_token { get; set; } 
        public DateTime? refresh_token_expiry { get; set; }
    }
}

