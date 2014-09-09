using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using CsvHelper.Configuration;

namespace KuaFu.NetEase
{
    public class StockDetail
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int StockDetailId { get; set; }

        public DateTime Date { get; set; }

        public string Symbol { get; set; }

        public string Name { get; set; }

        public double TodayClose { get; set; }

        public double High { get; set; }

        public double Low { get; set; }

        public double TodayOpen { get; set; }

        public double LastClose { get; set; }

        public double Change { get; set; }

        public double ChangeInPercent { get; set; }

        public double TurnOver { get; set; }

        public double VolumeTurnOver { get; set; }

        public double VolumeAmountTurnOver { get; set; }

        public double TotalCapitalization { get; set; }

        public double MovingCapitalization { get; set; }
    }

    public sealed class StockDetailMap : CsvClassMap<StockDetail>
    {
        public StockDetailMap()
        {
            //日期,股票代码,名称,收盘价,最高价,最低价,开盘价,前收盘,涨跌额,涨跌幅,换手率,成交量,成交金额,总市值,流通市值
            Map(m => m.Date).ConvertUsing(row => DateTime.Parse(row.GetField<string>("日期")));
            Map(m => m.Symbol).ConvertUsing(row => row.GetField<string>("股票代码").TrimStart('\''));
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
