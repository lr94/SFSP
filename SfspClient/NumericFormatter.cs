using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SfspClient
{
    internal static class NumericFormatter
    {
        public static string FormatBytes(long num)
        {
            if (num == 0)
                return "0 B";

            double n = num;
            string[] units = { "", "K", "M", "G", "T", "P", "E", "Z", "Y" };
            int ui = (int)Math.Truncate(Math.Log(n, 1024));

            n /= Math.Pow(1024, ui);

            string unit = units[ui];
            if (ui != 0)
                unit += "i";
            unit += "B";

            return String.Format("{0:0.#} {1}", n, unit);
        }

        public static string FormatTimeSpan(TimeSpan ts)
        {
            var sl = new List<string>();
            if (ts.Days != 0)
                sl.Add(ts.Days.ToString() + " giorn" + (ts.Days == 1 ? "o" : "i"));
            if (ts.Hours != 0)
                sl.Add(ts.Hours.ToString() + " or" + (ts.Hours == 1 ? "a" : "e"));
            if (ts.Minutes != 0)
                sl.Add(ts.Minutes.ToString() + " minut" + (ts.Minutes == 1 ? "o" : "i"));
            if (ts.Seconds != 0)
                sl.Add(ts.Seconds.ToString() + " second" + (ts.Seconds == 1 ? "o" : "i"));

            if (sl.Count == 0)
                return "";

            return sl.Aggregate((a, b) => a + ", " + b);
        }
    }
}
