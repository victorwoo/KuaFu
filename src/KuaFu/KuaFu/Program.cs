using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KuaFu
{
    class Program
    {
        static void Main(string[] args)
        {
            var iwencaiContext = new IwencaiContext();
            iwencaiContext.GetList<DailyStock>("20140906 涨跌幅 交易状态 开盘价 收盘价 最高价 最低价 成交量");
        }
    }
}
