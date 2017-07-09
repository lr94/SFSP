using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SfspClient
{
    internal abstract class NumericFormatter
    {
        public static string FormatBytes(long num)
        {
            if (num == 0)
                return "0 B";

            double n = num;
            string[] units = { "", "K", "M", "G", "T", "P", "E" };
            int ui = (int)Math.Truncate(Math.Log(n, 1024));

            n /= Math.Pow(1024, ui);

            string unit = units[ui];
            if (ui != 0)
                unit += "i";
            unit += "B";

            return String.Format("{0:0.#} {1}", n, unit);
        }
    }
}
