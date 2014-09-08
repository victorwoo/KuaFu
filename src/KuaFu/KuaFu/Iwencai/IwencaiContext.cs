using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace KuaFu.Iwencai
{
    internal class IwencaiContext
    {
        public IEnumerable<T> GetList<T>(string keyWord) where T : class, new()
        {
            int total;
            string token;
            GetDailyStockFromHtml(keyWord, out total, out token);
            var results = GetDailyStockFromAjax<T>(total, token);
            return results;
        }

        private IEnumerable<T> GetDailyStockFromAjax<T>(int total, string token) where T : class, new()
        {
            const string urlTemplate = "http://www.iwencai.com/stockpick/cache?token={0}&p={1}&perpage={2}";
            string url = string.Format(urlTemplate, token, 1, total);
            url = Uri.EscapeUriString(url);

            while (true)
            {
                try
                {
                    string responseString = HttpGet(url);
                    Debug.WriteLine(responseString);

                    var result = GetDailyStockFromAjaxResult<T>(responseString);
                    return result;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        private static void GetDailyStockFromHtml(string keyWord, out int total, out string token)
        {
            const string urlTemplate =
                "http://www.iwencai.com/stockpick/search?preParams=&ts=1&f=1&qs=1&selfsectsn=&querytype=&searchfilter=&tid=stockpick&w={0}";
            string url = string.Format(urlTemplate, keyWord);
            url = Uri.EscapeUriString(url);
            while (true)
            {
                try
                {
                    string responseString = HttpGet(url);
                    Debug.WriteLine(responseString);

                    GetDailyStockFromHtmlString(responseString, out total, out token);
                    return;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        private static void GetDailyStockFromHtmlString(string responseString, out int total, out string token)
        {
            string allResultString =
                    Regex.Match(responseString, "^var allResult = (?<ALL_RESULT>.*?);$",
                        RegexOptions.Multiline | RegexOptions.ExplicitCapture).Groups["ALL_RESULT"].Value;
            allResultString = Regex.Unescape(allResultString);
            var allResult = JObject.Parse(allResultString);
            total = (int)allResult["total"];
            token = (string)allResult["token"];
        }

        private static IEnumerable<T> GetDailyStockFromAjaxResult<T>(string responseString) where T: class, new()
        {
            var rootObject = JObject.Parse(responseString);
            var fieldTypes = (JArray)rootObject["fieldType"];
            var list = (JArray)rootObject["list"];
            var properties = typeof (T).GetProperties()
                .Where(item => item.GetCustomAttributes(typeof (IwencaiAttribute), true).Length > 0)
                .OrderBy(item =>
                {
                    var attribs = item.GetCustomAttributes(typeof (IwencaiAttribute), true);
                    return ((IwencaiAttribute) attribs[0]).Order;
                })
                .ToArray();
            for (int i = 0; i < list.Count; i++)
            {
                var t = new T();
                var fieldType = (string) fieldTypes[i];
                var itemString = (string) list[i];
                switch (fieldType)
                {
                    case "STR":
                        properties[i].SetValue(t, itemString, null);
                        break;
                    case "DOUBLE":
                        properties[i].SetValue(t, double.Parse(itemString), null);
                        break;
                    default:
                        break;
                }
                yield return t;
            }
        }

        private static string HttpGet(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = 10000;
            using (var resp = (HttpWebResponse)req.GetResponse())
            {
                var reader = new StreamReader(resp.GetResponseStream());
                return reader.ReadToEnd();
            }
        }
    }
}