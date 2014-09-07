using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KuaFu
{
    class DailyStock
    {
        /// <summary>
        /// 股票代码
        /// </summary>
        [Iwencai(0)]
        public string Name { get; set; }

        /// <summary>
        /// 股票简称
        /// </summary>
        [Iwencai(1)]
        public string Symbol { get; set; }

        /// <summary>
        /// 最新涨跌幅
        /// </summary>
        [Iwencai(2)]
        public double ChangeInPercentRealtime { get; set; }

        /// <summary>
        /// 最新价
        /// </summary>
        [Iwencai(3)]
        public double LastTradePriceOnly { get; set; }

        /// <summary>
        /// 涨跌幅
        /// </summary>
        [Iwencai(4)]
        public double ChangeInPercent { get; set; }

        /// <summary>
        /// 交易状态
        /// </summary>
        [Iwencai(5)]
        public string State { get; set; }

        /// <summary>
        /// 开盘价
        /// </summary>
        [Iwencai(6)]
        public double Open { get; set; }

        /// <summary>
        /// 收盘价
        /// </summary>
        [Iwencai(7)]
        public double Close { get; set; }

        /// <summary>
        /// 最高价
        /// </summary>
        [Iwencai(8)]
        public double DaysHigh { get; set; }

        /// <summary>
        /// 最低价
        /// </summary>
        [Iwencai(9)]
        public double DaysLow { get; set; }

        /// <summary>
        /// 成交量
        /// </summary>
        [Iwencai(10)]
        public double Volume { get; set; }
    }
}
