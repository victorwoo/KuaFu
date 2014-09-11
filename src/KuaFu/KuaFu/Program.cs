using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using KuaFu.NetEase;

namespace KuaFu
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Database.SetInitializer(
                new DropCreateDatabaseIfModelChanges<NetEaseDbContext>());

            NetEaseContext.DumpData();
        }
    }
}