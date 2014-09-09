using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KuaFu
{
    class Util
    {
        /// <summary>
        /// 查找工作日。
        /// </summary>
        /// <param name="start">起始日期。</param>
        /// <param name="offset">偏移量。1 - 下 1 个工作日；-2 - 前 2 个工作日</param>
        /// <returns>找到的工作日。</returns>
        static DateTime GetWorkDay(DateTime start, int offset)
        {
            for (int i = 0; i < Math.Abs(offset); i++)
            {
                do
                {
                    start = start.AddDays(Math.Sign(offset));
                } while ((int)start.DayOfWeek <= 0 && (int)start.DayOfWeek >= 6);
            }
            return start;
        }

        static DateTime GetTradeDay(DateTime start, int offset)
        {
            throw new NotImplementedException();
        }
    }
}
