using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsvHelper.Configuration;

namespace KuaFu.NetEase
{
    public class StockDetail
    {
        public string Date { get; set; }

        public string Symbol { get; set; }

        public string Name { get; set; }

        public string TodayClose { get; set; }

        public string High { get; set; }

        public string Low { get; set; }

        public string TodayOpen { get; set; }

        public string LastClose { get; set; }

        public string Change { get; set; }

        public string ChangeInPercent { get; set; }

        public string TurnOver { get; set; }

        public string VolumeTurnOver { get; set; }

        public string VolumeAmountTurnOver { get; set; }

        public string TotalCapitalization { get; set; }

        public string MovingCapitalization { get; set; }
    }

    public sealed class StockDetailMap : CsvClassMap<StockDetail>
    {
        public StockDetailMap ()
        {
            //日期,股票代码,名称,收盘价,最高价,最低价,开盘价,前收盘,涨跌额,涨跌幅,换手率,成交量,成交金额,总市值,流通市值
            Map(m => m.Date).Name("日期");
            Map(m => m.Symbol).Name("股票代码");

            Map(m => m.Name).Name("名称");
            Map(m => m.TodayClose).Name("收盘价");
            Map(m => m.High).Name("最高价");
            Map(m => m.Low).Name("最低价");
            Map(m => m.TodayOpen).Name("开盘价");
            Map(m => m.LastClose).Name("前收盘");
            Map(m => m.Change).Name("涨跌额");
            Map(m => m.ChangeInPercent).Name("涨跌幅");
            Map(m => m.TurnOver).Name("换手率");
            Map(m => m.VolumeTurnOver).Name("成交量");
            Map(m => m.VolumeAmountTurnOver).Name("成交金额");
            Map(m => m.TotalCapitalization).Name("总市值");
            Map(m => m.MovingCapitalization).Name("流通市值");
        }
    }
}
