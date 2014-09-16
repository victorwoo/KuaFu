using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using KuaFu.NetEase;

namespace KuaFu.Trade
{
    public class Trader1 : Trader
    {
        private static readonly Random Random = new Random();

        public Trader1(decimal stampTaxRate, decimal handlingCharge) : base(stampTaxRate, handlingCharge)
        {
        }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        /// <summary>
        ///     选股。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>股票代码。</returns>
        public string PickStock(DateTime date)
        {
            Console.WriteLine("选股。");
            List<string> pickedStocks;
            string[] avalailableSymbols = GetAvailableStocks();
            using (var db = new NetEaseDbContext())
            {
                DateTime? p1 = GetOpenDate(date, -1);
                if (p1 == null)
                {
                    return null;
                }

                IQueryable<string> symbols = db.StockInfoes
                    .Where(item => !avalailableSymbols.Contains(item.Symbol))
                    .Select(item => item.Symbol);
                Debug.WriteLine(symbols.ToString());
                IEnumerable<string> limitUpStocks = GetLimitUpStocks(p1.Value);
                pickedStocks = symbols.Intersect(limitUpStocks).ToList();
            }

            Console.WriteLine("选到 {0} 只好股票", pickedStocks.Count);
            if (pickedStocks.Count > 0)
            {
                int randomIndex = Random.Next(0, pickedStocks.Count);
                return pickedStocks[randomIndex];
            }

            return null;
        }

        public IEnumerable<string> GetLimitUpStocks(DateTime date)
        {
            Console.WriteLine("获取涨停股票。");
            using (var db = new NetEaseDbContext())
            {
                return db.StockDetails
                    .Where(
                        item => item.Date == date && item.ChangeInPercent > (decimal) 9.9 && item.ChangeInPercent < 11)
                    .Select(item => item.Symbol).ToList();
            }
        }

        public void Simulate()
        {
            DateTime startDate = DateTime.Parse("2014/06/01");
            DateTime endDate = DateTime.Parse("2014/09/01");

            for (DateTime currentDate = startDate; currentDate < endDate;)
            {
                Debug.WriteLine("{0:yy-MMM-dd ddd}", currentDate);
                // 把昨天买的卖掉。
                GetAvailableStocks().ToList().ForEach(item =>
                {
                    Position position = GetPosition(item);
                    if (position.LastBuyTime.Date == currentDate.Date.AddDays(-1))
                    {
                        Sell(item, currentDate + OpenTime);
                    }
                });

                // 买入选好的股票。
                string symbol = PickStock(currentDate);
                if (symbol != null)
                {
                    Buy(symbol, currentDate + OpenTime, Balance);
                }

                // 收盘时，统计总资产。
                decimal fullBalance = GetFullBalance(currentDate);
                Console.WriteLine("{0:yyyy-M-d dddd} 总资产为：{1:N2}", currentDate, fullBalance);

                // 日期增加。
                DateTime? nextDate = GetOpenDate(currentDate, 1);
                if (nextDate == null)
                {
                    break;
                }
                currentDate = nextDate.Value;
            }
        }
    }
}