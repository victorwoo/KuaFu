using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using KuaFu.Iwencai;
using KuaFu.Models;
using KuaFu.NetEase;

namespace KuaFu
{
    class Program
    {
        static void Main(string[] args)
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
            var stockInfos = NetEaseContext.GetStocks();
            foreach (var stockInfo in stockInfos)
            {
                Debug.WriteLine(stockInfo);
                var histories = NetEaseContext.GetHistory(stockInfo.Symbol, stockInfo.Code);
                foreach (var stockDetail in histories.Take(5))
                {
                    Debug.WriteLine("{0} {1} {2}", stockDetail.Date, stockDetail.Name, stockDetail.TodayClose);
                }
            }
        }
    }
}
