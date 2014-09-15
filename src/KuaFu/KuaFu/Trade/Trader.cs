using System;
using System.Collections.Generic;
using System.Linq;
using KuaFu.NetEase;

namespace KuaFu.Trade
{
    public class Trader
    {
        public class Position
        {
            public int Volume { get; set; }
            
            public DateTime LastBuyTime { get; set; }
        }

        private static IEnumerable<DateTime> _openDates;

        private static readonly IDictionary<string, IEnumerable<DateTime>> OpenDatesCache =
            new Dictionary<string, IEnumerable<DateTime>>();

        public static readonly TimeSpan OpenTime = TimeSpan.Parse("9:30");

        public static readonly TimeSpan NoonTime = TimeSpan.Parse("11:30");

        public static readonly TimeSpan CloseTime = TimeSpan.Parse("11:30");

        private readonly IDictionary<string, Position> _positions = new Dictionary<string, Position>();

        /// <summary>
        /// </summary>
        /// <param name="stampTaxRate">印花税率。</param>
        /// <param name="handlingCharge">手续费。</param>
        public Trader(decimal stampTaxRate, decimal handlingCharge)
        {
            StampTaxRate = stampTaxRate;
            HandlingCharge = handlingCharge;
        }

        private decimal StampTaxRate { get; set; }

        private decimal HandlingCharge { get; set; }

        /// <summary>
        ///     余额。
        /// </summary>
        public decimal Balance { get; set; }

        protected Position GetPosition(string symbol)
        {
            return _positions.ContainsKey(symbol) ? null : _positions[symbol];
        }

        protected string[] GetAvailableStocks()
        {
            return _positions.Keys.ToArray();
        }

        /// <summary>
        ///     调整仓位。
        /// </summary>
        /// <param name="datetime">操作时间。</param>
        /// <param name="symbol">股票代码。</param>
        /// <param name="volumeChange">调整值。单位为股数。正数为加仓，负数为减仓。</param>
        private void ChangePosition(DateTime datetime, string symbol, int volumeChange)
        {
            if (!_positions.ContainsKey(symbol))
            {
                _positions.Add(symbol, new Position {LastBuyTime = datetime, Volume = volumeChange});
            }
            else
            {
                int position = _positions[symbol].Volume + volumeChange;
                if (position == 0)
                {
                    _positions.Remove(symbol);
                }
                else
                {
                    _positions[symbol].LastBuyTime = datetime;
                    _positions[symbol].Volume = position;
                }
            }
        }

        public void Initialize()
        {
            using (var db = new NetEaseDbContext())
            {
                IQueryable<TradeInfo> all = from tradeInfo in db.TradInfoes select tradeInfo;
                db.TradInfoes.RemoveRange(all);
                db.SaveChanges();
            }
        }

        public decimal Buy(string symbol, DateTime time, decimal maxAmount)
        {
            if (time.TimeOfDay != OpenTime && time.TimeOfDay != CloseTime)
            {
                throw new ArgumentOutOfRangeException("time", "只支持开盘时间或收盘时间。");
            }
            using (var db = new NetEaseDbContext())
            {
                StockDetail stockDetail = db.StockDetails.Single(item => item.Symbol == symbol && item.Date == time.Date);
                decimal price = time.TimeOfDay == OpenTime ? stockDetail.TodayOpen : stockDetail.TodayClose;
                var hands = (int) Math.Floor(maxAmount/price/100);
                int volume = hands*100;
                decimal amount = price*volume;
                decimal balanceBefore = Balance;
                decimal balanceAfter = Balance - amount - HandlingCharge;
                Balance = balanceAfter;
                Console.Write("{0:yyyy-M-d dddd hh:NN:ss} 买入 [{1}] {2} 股，余额 {3:N2}", time, symbol, volume, balanceAfter);
                var tradeInfo = new TradeInfo
                {
                    BalanceAfter = balanceAfter,
                    BalanceBefore = balanceBefore,
                    HandlingCharge = HandlingCharge,
                    Price = price,
                    Symbol = symbol,
                    Time = time,
                    TotalAmount = -amount,
                    Volume = -volume
                };
                db.TradInfoes.Add(tradeInfo);
                db.SaveChanges();
                ChangePosition(time, symbol, volume);
            }
            return Balance;
        }

        public decimal Sell(string symbol, DateTime time)
        {
            if (time.TimeOfDay != OpenTime && time.TimeOfDay != CloseTime)
            {
                throw new ArgumentOutOfRangeException("time", "只支持开盘时间或收盘时间。");
            }
            using (var db = new NetEaseDbContext())
            {
                StockDetail stockDetail = db.StockDetails.Single(item => item.Symbol == symbol && item.Date == time.Date);
                decimal price = time.TimeOfDay == OpenTime ? stockDetail.TodayOpen : stockDetail.TodayClose;
                int volume = GetPosition(symbol).Volume;
                decimal amount = price*volume;
                decimal balanceBefore = Balance;
                decimal stampTax = amount*StampTaxRate;
                decimal balanceAfter = Balance + amount - HandlingCharge - stampTax;
                Balance = balanceAfter;
                var tradeInfo = new TradeInfo
                {
                    BalanceAfter = balanceAfter,
                    BalanceBefore = balanceBefore,
                    HandlingCharge = HandlingCharge,
                    Price = price,
                    Symbol = symbol,
                    Time = time,
                    TotalAmount = amount,
                    Volume = volume
                };
                db.TradInfoes.Add(tradeInfo);
                db.SaveChanges();
                ChangePosition(time, symbol, -volume);
            }
            return Balance;
        }

        /// <summary>
        ///     获取交易日。
        /// </summary>
        /// <param name="symbol">股票代码。</param>
        /// <param name="fromDate">起始日期。</param>
        /// <param name="offset">
        ///     交易日个数。
        ///     正数表示向后（forward）第 n 个交易日；负数表示向前（backward）第 n 个交易日。
        /// </param>
        /// <returns></returns>
        public static DateTime? GetOpenDate(string symbol, DateTime fromDate, int offset)
        {
            IList<DateTime> openDates;
            if (!OpenDatesCache.ContainsKey(symbol))
            {
                openDates = GetOpenDates(symbol).ToList();
                OpenDatesCache.Add(symbol, openDates);
            }
            else
            {
                openDates = OpenDatesCache[symbol].ToList();
            }

            openDates = openDates.SkipWhile(item => item < fromDate).ToList();
            return openDates.First();
        }

        public static DateTime? GetOpenDate(DateTime fromDate, int offset)
        {
            if (_openDates == null)
            {
                _openDates = GetOpenDates();
            }

            IList<DateTime> openDateList = _openDates as IList<DateTime> ?? _openDates.ToList();
            openDateList = openDateList.SkipWhile(item => item < fromDate).ToList();
            return openDateList.First();
        }

        /// <summary>
        ///     获取所有历史上的交易日。
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<DateTime> GetOpenDates()
        {
            using (var db = new NetEaseDbContext())
            {
                return db.StockDetails.GroupBy(item => item.Date).Select(item => item.Key).ToList();
            }
        }

        /// <summary>
        ///     获取某只股票的所有交易日。
        /// </summary>
        /// <param name="symbol">股票代码。</param>
        /// <returns>该只股票的所有交易日。</returns>
        public static IEnumerable<DateTime> GetOpenDates(string symbol)
        {
            using (var db = new NetEaseDbContext())
            {
                return
                    db.StockDetails.Where(item => item.Symbol == symbol)
                        .GroupBy(item => item.Date)
                        .Select(item => item.Key);
            }
        }

        public decimal GetFullBalance(DateTime dateTime)
        {
            decimal sum = 0;
            using (var db = new NetEaseDbContext())
            {
                this._positions.Keys.ToList().ForEach(symbol =>
                {
                    var position = GetPosition(symbol);
                    var detail = db.StockDetails.Single(item => item.Symbol == symbol && item.Date == dateTime);
                    sum += detail.TodayClose*position.Volume;
                });
            }

            return sum;
        }
    }
}