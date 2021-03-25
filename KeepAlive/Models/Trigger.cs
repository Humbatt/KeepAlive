using System;
using System.Collections.Generic;
using System.Text;

namespace KeepAlive.Models
{
    public class Trigger
    {
       public  SiteConfig Site { get; set; }

       public DateTime Due { get; set; }
    }
}
