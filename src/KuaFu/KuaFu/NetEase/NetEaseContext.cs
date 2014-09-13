﻿using System;
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
                Debug.WriteLine(string.Format("耗时: {0:N0} 秒", during.TotalSeconds));
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
                Debug.WriteLine(string.Format("耗时: {0:N0} 秒", during.TotalSeconds));
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

        private static void GetDateRangeFromServer(string symbol, out DateTime startDate, out DateTime endDate)
        {
            Console.Write("获取日期范围");
            while (true)
            {
                try
                {
                    string url = "http://quotes.money.163.com/trade/lsjysj_{0}.html";
                    url = String.Format(url, symbol);

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

        private static void GetDateRangeFromDatabase(string symbol, out DateTime? startDate, out DateTime? endDate)
        {
            using (var db = new NetEaseDbContext())
            {
                var query =
                    db.StockDetails.Where(stockDetail => stockDetail.Symbol == symbol)
                        .GroupBy(stockDetail => stockDetail.Symbol)
                        .Select(group => new
                        {
                            MaxDate = @group.Max(item => item.Date),
                            MinDate = @group.Min(item => item.Date)
                        });
                var result = query.FirstOrDefault();
                if (result == null)
                {
                    startDate = null;
                    endDate = null;
                }
                else
                {
                    startDate = result.MinDate;
                    endDate = result.MaxDate;
                }
            }
        }

        public static IEnumerable<StockDetail> GetHistories(string symbol, string code)
        {
            DateTime startDate1;
            DateTime endDate1;
            GetDateRangeFromServer(symbol, out startDate1, out endDate1);

            DateTime? startDate2;
            DateTime? endDate2;
            GetDateRangeFromDatabase(symbol, out startDate2, out endDate2);

            IEnumerable<StockDetail> results = GetCsv(code, startDate1, endDate1);
            if (startDate2 != null && endDate2 != null)
            {
                results = results.Where(item => item.Date < startDate2.Value || item.Date > endDate2.Value);
            }
            return results;
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
                    url = String.Format(url, code, startDate, endDate);
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
                    Console.WriteLine(" {0:N2} 秒，{1:N0} 条数据", during.TotalSeconds, result.Count);
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

        public static void DumpData()
        {
            IEnumerable<StockInfo> stockInfoes = GetStocks();
            int i = 0;
            foreach (StockInfo stockInfo in stockInfoes)
            {
                Console.WriteLine();
                Console.WriteLine("添加第 {0} 支股票（{1}）信息", ++i, stockInfo.Symbol);
                using (var db = new NetEaseDbContext())
                {
                    var original = db.StockInfoes.SingleOrDefault(item => item.Code == stockInfo.Code);
                    if (original != null)
                    {
                        // 如果已有本支股票信息
                        original.Symbol = stockInfo.Symbol;
                        original.Name = stockInfo.Name;
                    }
                    else
                    {
                        // 如果没有本支股票信息
                        db.StockInfoes.Add(stockInfo);
                    }

                    IEnumerable<StockDetail> histories = GetHistories(stockInfo.Symbol, stockInfo.Code);
                    Console.Write("保存明细至数据库");
                    DateTime start = DateTime.Now;
                    int count = 0;
                    foreach (StockDetail stockDetail in histories /*.Take(5)*/)
                    {
                        db.StockDetails.Add(stockDetail);
                        count++;
                    }

                    db.SaveChanges();
                    TimeSpan during = DateTime.Now - start;

                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write(" [成功]");
                    Console.ResetColor();
                    Console.WriteLine(" {0:N2} 秒，{1:N0} 条有效数据", during.TotalSeconds, count);
                }
            }
        }
    }
}