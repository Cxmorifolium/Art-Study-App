﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace artstudio.Models
{
    public class UnsplashImage
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public Urls? urls { get; set; }
        public User? user { get; set; }

        public class Urls
        {
            public string? Raw { get; set; }
            public string? Full { get; set; }
            public string? Regular { get; set; }
            public string? Small { get; set; }
            public string? Thumb { get; set; }
        }

        public class User
        {
            public string? Name { get; set; }
            public string? PortfolioUrl { get; set; }
            public string? Username { get; set; }
            public string UnsplashProfileUrl => !string.IsNullOrEmpty(Username)
        ? $"https://unsplash.com/@{Username}"
        : "https://unsplash.com";
        }
    }
}
