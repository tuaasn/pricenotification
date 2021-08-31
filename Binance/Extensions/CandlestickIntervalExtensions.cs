using System;

// ReSharper disable once CheckNamespace
namespace Binance
{
    public static class CandlestickIntervalExtensions
    {
        /// <summary>
        /// Convert <see cref="CandlestickInterval"/> to string.
        /// </summary>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static string AsString(this CandlestickInterval interval)
        {
            switch (interval)
            {
                case CandlestickInterval.Minute: return "1m";
                case CandlestickInterval.Minutes_3: return "3m";
                case CandlestickInterval.Minutes_5: return "5m";
                case CandlestickInterval.Minutes_15: return "15m";
                case CandlestickInterval.Minutes_30: return "30m";
                case CandlestickInterval.Hour: return "1h";
                case CandlestickInterval.Hours_2: return "2h";
                case CandlestickInterval.Hours_4: return "4h";
                case CandlestickInterval.Hours_6: return "6h";
                case CandlestickInterval.Hours_8: return "8h";
                case CandlestickInterval.Hours_12: return "12h";
                case CandlestickInterval.Day: return "1d";
                case CandlestickInterval.Days_3: return "3d";
                case CandlestickInterval.Week: return "1w";
                case CandlestickInterval.Month: return "1M";
                default:
                    throw new ArgumentException($"{nameof(CandlestickIntervalExtensions)}.{nameof(AsString)}: {nameof(CandlestickInterval)} not supported: {interval}");
            }
        }

        public static CandlestickInterval AsCandlestickInterval(this string interval)
        {
            switch (interval)
            {
                case "1m": return CandlestickInterval.Minute;
                case "3m": return CandlestickInterval.Minutes_3;
                case "5m": return CandlestickInterval.Minutes_5;
                case "15m": return CandlestickInterval.Minutes_15;
                case "30m": return CandlestickInterval.Minutes_30;
                case "1h": return CandlestickInterval.Hour;
                case "2h": return CandlestickInterval.Hours_2;
                case "4h": return CandlestickInterval.Hours_4;
                case "6h": return CandlestickInterval.Hours_6;
                case "8h": return CandlestickInterval.Hours_8;
                case "12h": return CandlestickInterval.Hours_12;
                case "1d": return CandlestickInterval.Day;
                case "3d": return CandlestickInterval.Days_3;
                case "1w": return CandlestickInterval.Week;
                case "1M": return CandlestickInterval.Month;
                default:
                    throw new ArgumentException($"{nameof(CandlestickIntervalExtensions)}.{nameof(AsString)}: {nameof(CandlestickInterval)} not supported: {interval}");
            }
        }
    }
}
