namespace KuaFu.Iwencai
{
    public class AjaxResponse
    {
        public string[] title { get; set; }
        public string[][] list { get; set; }
        public string[] indexID { get; set; }
        public string[] fieldType { get; set; }
        public string[] showType { get; set; }
        public string queryType { get; set; }
        public object[] processConf { get; set; }
        public Querystring queryString { get; set; }
        public object[] orresProcess { get; set; }
        public Extresprocess extresProcess { get; set; }
        public object[] titleProcess { get; set; }
        public Extend extend { get; set; }
        public Conf conf { get; set; }
        public bool checkbox { get; set; }
    }

    public class Querystring
    {
        public string token { get; set; }
        public string p { get; set; }
        public string perpage { get; set; }
        public int my { get; set; }
        public string w { get; set; }
        public string cid { get; set; }
        public string username { get; set; }
        public string spi { get; set; }
    }

    public class Extresprocess
    {
        public _1 _1 { get; set; }
    }

    public class _1
    {
        public string[] graph { get; set; }
        public object[] interest { get; set; }
    }

    public class Extend
    {
        public object[] interest { get; set; }
    }

    public class Conf
    {
        public Interest interest { get; set; }
    }

    public class Interest
    {
        public int colIndex { get; set; }
    }
}