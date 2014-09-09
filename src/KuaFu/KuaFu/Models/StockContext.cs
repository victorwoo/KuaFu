using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace KuaFu.Models
{
    public class StockContext : DbContext
    {
        public StockContext()
            : base("DefaultConnection")
        {
        }

        public DbSet<DailyStock> DailyStocks { get; set; }
    }
}
