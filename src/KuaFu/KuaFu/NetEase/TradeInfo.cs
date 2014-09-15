using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace KuaFu.NetEase
{
    public class TradeInfo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TradeId { get; set; }

        [Index(IsUnique = false)]
        public DateTime Time { get; set; }

        /// <summary>
        /// 股票代码。
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// 股数。
        /// 正数为卖出，负数为买入。
        /// </summary>
        public int Volume { get; set; }

        /// <summary>
        /// 交易价格。
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 发生金额。
        /// 包含手续费。正数为收入，负数为支出。
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 手续费。
        /// </summary>
        public decimal HandlingCharge { get; set; }

        /// <summary>
        /// 交易前余额。
        /// </summary>
        public decimal BalanceBefore { get; set; }

        /// <summary>
        /// 交易后余额。
        /// </summary>
        public decimal BalanceAfter { get; set; }
    }
}
