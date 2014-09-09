using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace KuaFu.NetEase
{
    public class NetEaseDbContext : DbContext
    {
        public NetEaseDbContext()
            : base("DefaultConnection")
        {
        }

        public DbSet<StockInfo> StockInfoes { get; set; }

        public DbSet<StockDetail> StockDetails { get; set; }
    }
}
