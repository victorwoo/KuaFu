using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using KuaFu.NetEase;

namespace KuaFu
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //var iwencaiContext = new IwencaiContext();
            //using (var db = new StockContext())
            //{
            //    var success = false;
            //    while (!success)
            //    {
            //        try
            //        {
            //            var items = iwencaiContext.GetList<DailyStock>("20140906 涨跌幅 交易状态 开盘价 收盘价 最高价 最低价 成交量");
            //            foreach (var dailyStock in items)
            //            {
            //                Debug.WriteLine(dailyStock.Symbol);
            //                db.DailyStocks.Add(dailyStock);
            //                db.SaveChanges();
            //            }
            //            success = true;
            //        }
            //        catch
            //        {

            //        }
            //    }
            //}

            Database.SetInitializer(
                new DropCreateDatabaseIfModelChanges<NetEaseDbContext>());

            Console.Write("清除脏数据");
            using (var db = new NetEaseDbContext())
            {
                foreach (StockInfo stockInfo in db.StockInfoes.Where(item => !item.IsCompleted))
                {
                    string symbol = stockInfo.Symbol;
                    IQueryable<StockDetail> dirtyDetails = db.StockDetails.Where(item => item.Symbol == symbol);
                    db.StockDetails.RemoveRange(dirtyDetails);
                    db.StockInfoes.Remove(stockInfo);
                }
            }
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(" [成功]");
            Console.ResetColor();

            IEnumerable<StockInfo> stockInfoes = NetEaseContext.GetStocks();
            int i = 0;
            foreach (StockInfo stockInfo in stockInfoes)
            {
                Console.WriteLine();
                Console.WriteLine("添加第 {0} 支股票（{1}）信息", i++, stockInfo.Symbol);
                using (var db = new NetEaseDbContext())
                {
                    if (db.StockInfoes.Any(item => item.Code == stockInfo.Code))
                    {
                        Console.WriteLine("跳过已有数据");
                        continue;
                    }

                    //if (!db.StockInfoes.Any(item => item.Code == stockInfo.Code))
                    //{
                    db.StockInfoes.Add(stockInfo);
                    //}

                    IEnumerable<StockDetail> histories = NetEaseContext.GetHistory(stockInfo.Symbol, stockInfo.Code);
                    Console.Write("保存明细至数据库");
                    DateTime start = DateTime.Now;
                    foreach (StockDetail stockDetail in histories /*.Take(5)*/)
                    {
                        //Debug.WriteLine("{0} {1} {2}", stockDetail.Date, stockDetail.Name, stockDetail.TodayClose);
                        //if (!db.StockDetails.Any(
                        //    item => item.Symbol == stockDetail.Symbol && item.Date == stockDetail.Date))
                        //{
                        db.StockDetails.Add(stockDetail);
                        //}
                    }

                    stockInfo.IsCompleted = true;
                    db.SaveChanges();
                    TimeSpan during = DateTime.Now - start;

                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write(" [成功]");
                    Console.ResetColor();
                    Console.WriteLine(" {0} 秒", during.TotalSeconds);
                }
            }
        }
    }
}