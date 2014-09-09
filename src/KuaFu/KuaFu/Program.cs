using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
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
            IEnumerable<StockInfo> stockInfoes = NetEaseContext.GetStocks();
            foreach (StockInfo stockInfo in stockInfoes)
            {
                using (var db = new NetEaseDbContext())
                {
                    if (!db.StockInfoes.Any(item => item.Code == stockInfo.Code))
                    {
                        db.StockInfoes.Add(stockInfo);
                    }

                    IEnumerable<StockDetail> histories = NetEaseContext.GetHistory(stockInfo.Symbol, stockInfo.Code);
                    foreach (StockDetail stockDetail in histories/*.Take(5)*/)
                    {
                        Debug.WriteLine("{0} {1} {2}", stockDetail.Date, stockDetail.Name, stockDetail.TodayClose);
                        if (!db.StockDetails.Any(
                            item => item.Symbol == stockDetail.Symbol && item.Date == stockDetail.Date))
                        {
                            db.StockDetails.Add(stockDetail);
                        }
                    }

                    db.SaveChanges();
                }
            }
        }
    }
}