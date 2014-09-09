using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using CsvHelper;
using KuaFu.NetEase.Json;
using Newtonsoft.Json;

namespace KuaFu.NetEase
{
    public class NetEaseContext
    {
        private static string HttpGet(string url, int timeout = 5000)
        {
            Debug.WriteLine("HTTP GET: " + url);
            var start = DateTime.Now;
            var req = (HttpWebRequest) WebRequest.Create(url);
            req.Timeout = timeout;
            using (var resp = (HttpWebResponse) req.GetResponse())
            {
                var reader = new StreamReader(resp.GetResponseStream());
                var result = reader.ReadToEnd();
                var during = DateTime.Now - start;
                Debug.WriteLine("耗时: " + during.TotalSeconds + " 秒");
                return result;
            }
        }

        private static byte[] HttpGet2(string url, int timeout = 5000)
        {
            Debug.WriteLine("HTTP GET: " + url);
            var start = DateTime.Now;
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = timeout;
            using (var resp = (HttpWebResponse)req.GetResponse())
            {
                var ms = new MemoryStream();
                resp.GetResponseStream().CopyTo(ms);
                var during = DateTime.Now - start;
                Debug.WriteLine("耗时: " + during.TotalSeconds + " 秒");
                var array = ms.ToArray();
                return array;
            }
        }


        public static IEnumerable<StockInfo> GetStocks()
        {
            const string url =
                "http://quotes.money.163.com/hs/service/marketradar_ajax.php?host=http://quotes.money.163.com/hs/service/marketradar_ajax.php&page=0&query=STYPE:EQA&types=&count=3&type=query";
            string json = HttpGet(url, 10000);

            var rootObject = JsonConvert.DeserializeObject<Rootobject>(json);
            return rootObject.list.Select(item => new StockInfo
            {
                Code = item.CODE,
                Symbol = item.SYMBOL,
                Name = item.NAME
            });
        }

        public static IEnumerable<StockDetail> GetHistory(string symbol, string code)
        {
            DateTime startDate;
            DateTime endDate;
            GetDateRange(symbol, out startDate, out endDate);

            Debug.WriteLine(startDate);
            Debug.WriteLine(endDate);

            var results = GetCsv(code, startDate, endDate);
            return results;
        }

        private static void GetDateRange(string symbol, out DateTime startDate, out DateTime endDate)
        {
            string url = "http://quotes.money.163.com/trade/lsjysj_{0}.html";
            url = string.Format(url, symbol);

            string html = HttpGet(url, 10000);
            string dateStartType = Regex.Match(
                html,
                @"<input type=""radio"" name=""date_start_type"" value=""(?<DATE_START_TYPE>\d{4}-\d{2}-\d{2})"" >",
                RegexOptions.Multiline | RegexOptions.ExplicitCapture
                ).Groups["DATE_START_TYPE"].Value;
            startDate = DateTime.Parse(dateStartType);

            string dateEndType = Regex.Match(
                html,
                @"<input type=""radio"" name=""date_end_type"" value=""(?<DATE_END_TYPE>\d{4}-\d{2}-\d{2})"">",
                RegexOptions.Multiline | RegexOptions.ExplicitCapture
                ).Groups["DATE_END_TYPE"].Value;
            endDate = DateTime.Parse(dateEndType);
        }

        private static IEnumerable<StockDetail> GetCsv(string code, DateTime startDate, DateTime endDate)
        {
            string url =
                "http://quotes.money.163.com/service/chddata.html?code={0}&start={1:yyyyMMdd}&end={2:yyyyMMdd}&fields=TCLOSE;HIGH;LOW;TOPEN;LCLOSE;CHG;PCHG;TURNOVER;VOTURNOVER;VATURNOVER;TCAP;MCAP";
            url = string.Format(url, code, startDate, endDate);
            var response = HttpGet2(url, 15000);
            var responseString = System.Text.Encoding.GetEncoding("GBK").GetString(response);
            var csv = new CsvReader(new StringReader(responseString));
            csv.Configuration.RegisterClassMap<StockDetailMap>();
            while (csv.Read())
            {
                var record = csv.GetRecord<StockDetail>();
                yield return record;
            }
        }
    }
}