using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SfspClient
{
    internal static class NumericFormatter
    {
        /// <summary>
        /// Formatta un numero di byte usando l'opportuna unità di misura.
        /// Le unità utilizzate sono quelle definite dalla IEC nel 1998, su base 1024
        /// (KiB, MiB, GiB...)
        /// In teoria si chiamerebbero "kibibyte", "mebibyte" ecc ecc
        /// </summary>
        /// <param name="num">Numero di byte</param>
        /// <returns>Stringa contenente il numero di byte (con una cifra decimale massima) e l'unità di misura</returns>
        public static string FormatBytes(long num)
        {
            if (num == 0)
                return "0 B";

            double n = num;
            string[] units = { "", "K", "M", "G", "T", "P", "E", "Z", "Y" };
            int ui = (int)Math.Truncate(Math.Log(n, 1024));

            n /= Math.Pow(1024, ui);

            string unit = ui < units.Length ? units[ui] : "?";
            if (ui != 0)
                unit += "i";
            unit += "B";

            return String.Format("{0:0.#} {1}", n, unit);
        }

        /// <summary>
        /// Formatta un intervallo di tempo esprimendolo in italiano
        /// </summary>
        /// <param name="ts">Intervallo temporale</param>
        /// <returns>Una stringa rappresentante l'intervallo di tempo in italiano</returns>
        public static string FormatTimeSpan(TimeSpan ts)
        {
            if(ts.TotalSeconds < 1.0)
                return ts.Milliseconds + " millisecond" + (ts.Milliseconds == 1 ? "o" : "i");

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
