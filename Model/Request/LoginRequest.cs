﻿using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
