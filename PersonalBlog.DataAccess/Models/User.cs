﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalBlog.DataAccess.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }

        public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}