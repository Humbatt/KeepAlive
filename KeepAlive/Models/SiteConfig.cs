using System;
using System.Collections.Generic;
using System.Text;

namespace KeepAlive.Models
{
    public class SiteConfig
    {
        public string Url { get; set; }

        public bool IsSiteMap { get; set; }

        public int Interval { get; set; }
    }
}
