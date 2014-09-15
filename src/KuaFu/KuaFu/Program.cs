using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using KuaFu.NetEase;
using KuaFu.Trade;

namespace KuaFu
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Database.SetInitializer(
                new DropCreateDatabaseIfModelChanges<NetEaseDbContext>());

            //NetEaseContext.DumpData();

            var trader = new Trader1(1/1000, 5);
            trader.Initialize();
            trader.Simulate();
        }
    }
}