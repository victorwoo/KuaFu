using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
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
            Thread.Sleep(1000);
            IEnumerable<T> results = GetDailyStockFromAjax<T>(total, token);
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
                    IEnumerable<T> result = GetDailyStockFromAjaxResult<T>(responseString);
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
                    string responseString = HttpGet(url, 15000);

                    GetDailyStockFromHtmlString(responseString, out total, out token);
                    if (total != 0)
                    {
                        return;
                    }
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
            Debug.WriteLine(allResultString);

            JObject allResult = JObject.Parse(allResultString);
            total = (int) allResult["total"];
            token = (string) allResult["token"];
        }

        private static IEnumerable<T> GetDailyStockFromAjaxResult<T>(string responseString) where T : class, new()
        {
            Debug.WriteLine(Regex.Unescape(responseString));
            JObject rootObject = JObject.Parse(responseString);
            var fieldTypes = (JArray) rootObject["fieldType"];
            var list = (JArray) rootObject["list"];
            PropertyInfo[] properties = typeof (T).GetProperties()
                .Where(item => item.GetCustomAttributes(typeof (IwencaiAttribute), true).Length > 0)
                .OrderBy(item =>
                {
                    object[] attribs = item.GetCustomAttributes(typeof (IwencaiAttribute), true);
                    return ((IwencaiAttribute) attribs[0]).Order;
                })
                .ToArray();
            foreach (JToken t1 in list)
            {
                var t = new T();
                var itemStrings = t1.Select(item=>item.Value<string>()).ToArray();

                for (int j = 0; j < fieldTypes.Count; j++)
                {
                    var fieldType = (string)fieldTypes[j];
                    switch (fieldType)
                    {
                        case "STR":
                            properties[j].SetValue(t, itemStrings[j], null);
                            break;
                        case "DOUBLE":
                            double value;
                            if (!double.TryParse(itemStrings[j], out value))
                            {
                                value = double.NaN;
                            }
                            properties[j].SetValue(t, value, null);
                            break;
                    }
                }
                
                yield return t;
            }
            //return null;
        }

        private static string HttpGet(string url, int timeout = 5000)
        {
            Debug.WriteLine(url);
            var req = (HttpWebRequest) WebRequest.Create(url);
            req.Timeout = timeout;
            using (var resp = (HttpWebResponse) req.GetResponse())
            {
                var reader = new StreamReader(resp.GetResponseStream());
                return reader.ReadToEnd();
            }
        }
    }
}