using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KuaFu.NetEase.Json
{
    public class Rootobject
    {
        public int page { get; set; }
        public int count { get; set; }
        public int total { get; set; }
        public int pagecount { get; set; }
        public string time { get; set; }
        public List[] list { get; set; }
    }
}
