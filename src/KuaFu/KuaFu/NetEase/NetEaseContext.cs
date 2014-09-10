using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
            DateTime start = DateTime.Now;
            var req = (HttpWebRequest) WebRequest.Create(url);
            req.Timeout = timeout;
            using (var resp = (HttpWebResponse) req.GetResponse())
            {
                var reader = new StreamReader(resp.GetResponseStream());
                string result = reader.ReadToEnd();
                TimeSpan during = DateTime.Now - start;
                Debug.WriteLine("耗时: " + during.TotalSeconds + " 秒");
                return result;
            }
        }

        private static byte[] HttpGet2(string url, int timeout = 5000)
        {
            Debug.WriteLine("HTTP GET: " + url);
            DateTime start = DateTime.Now;
            var req = (HttpWebRequest) WebRequest.Create(url);
            req.Timeout = timeout;
            using (var resp = (HttpWebResponse) req.GetResponse())
            {
                var ms = new MemoryStream();
// ReSharper disable once PossibleNullReferenceException
                resp.GetResponseStream().CopyTo(ms);
                TimeSpan during = DateTime.Now - start;
                Debug.WriteLine("耗时: " + during.TotalSeconds + " 秒");
                byte[] array = ms.ToArray();
                return array;
            }
        }


        public static IEnumerable<StockInfo> GetStocks()
        {
            Console.Write("获取股票列表");
            while (true)
            {
                try
                {
                    const string url =
                        "http://quotes.money.163.com/hs/service/marketradar_ajax.php?host=http://quotes.money.163.com/hs/service/marketradar_ajax.php&page=0&query=STYPE:EQA&types=&count=9999&type=query";
                    string json = HttpGet(url, 10000);

                    var rootObject = JsonConvert.DeserializeObject<Rootobject>(json);
                    var result = rootObject.list.Select(item => new StockInfo
                    {
                        Code = item.CODE,
                        Symbol = item.SYMBOL,
                        Name = item.NAME
                    });
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write(" [成功]");
                    Console.ResetColor();
                    Console.WriteLine(" {0} 条数据", rootObject.list.Length);
                    return result;
                }
// ReSharper disable once EmptyGeneralCatchClause
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(" [失败]");
                    Console.ResetColor();
                }
            }
        }

        public static IEnumerable<StockDetail> GetHistory(string symbol, string code)
        {
            DateTime startDate;
            DateTime endDate;
            GetDateRange(symbol, out startDate, out endDate);

            //Debug.WriteLine(startDate);
            //Debug.WriteLine(endDate);

            IEnumerable<StockDetail> results = GetCsv(code, startDate, endDate);
            return results;
        }

        private static void GetDateRange(string symbol, out DateTime startDate, out DateTime endDate)
        {
            Console.Write("获取日期范围");
            while (true)
            {
                try
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

                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write(" [成功]");
                    Console.ResetColor();
                    Console.WriteLine(" {0} - {1}", startDate.ToShortDateString(), endDate.ToShortDateString());
                    return;
                }
// ReSharper disable once EmptyGeneralCatchClause
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(" [失败]");
                    Console.ResetColor();
                }
            }
        }

        private static IEnumerable<StockDetail> GetCsv(string code, DateTime startDate, DateTime endDate)
        {
            Console.Write("获取个股历史明细");
            var result = new List<StockDetail>();
            while (true)
            {
                try
                {
                    var start = DateTime.Now;
                    string url =
                        "http://quotes.money.163.com/service/chddata.html?code={0}&start={1:yyyyMMdd}&end={2:yyyyMMdd}&fields=TCLOSE;HIGH;LOW;TOPEN;LCLOSE;CHG;PCHG;TURNOVER;VOTURNOVER;VATURNOVER;TCAP;MCAP";
                    url = string.Format(url, code, startDate, endDate);
                    byte[] response = HttpGet2(url, 15000);
                    string responseString = Encoding.GetEncoding("GBK").GetString(response);
                    var csv = new CsvReader(new StringReader(responseString));
                    csv.Configuration.RegisterClassMap<StockDetailMap>();
                    while (csv.Read())
                    {
                        var record = csv.GetRecord<StockDetail>();
                        result.Add(record);
                    }

                    var during = DateTime.Now - start;

                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write(" [成功]");
                    Console.ResetColor();
                    Console.WriteLine(" {0} 秒，{1} 条数据", during.TotalSeconds, result.Count);
                    return result;
                }
// ReSharper disable once EmptyGeneralCatchClause
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(" [失败]");
                    Console.ResetColor();
                }
            }
        }
    }
}